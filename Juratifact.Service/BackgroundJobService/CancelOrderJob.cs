using Quartz;
using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Juratifact.Service.BackgroundJobService;

[DisallowConcurrentExecution]
public class CancelOrderJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;

    public CancelOrderJob(AppDbContext dbContext, ILogger<CancelOrderJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Đang chạy CancelOrderJob: quét các đơn hàng hết hạn thanh toán...");

        var now = DateTimeOffset.UtcNow;
        var timeoutThreshold = now.AddMinutes(-10);

        // Luồng order mới tạo nhiều SellerOrders; khi hết hạn thanh toán cần hủy parent order + toàn bộ seller orders,
        // đánh dấu transaction pending là Expired, và trả product về Available.
        var orderIdsToCancel = await _dbContext.Transactions
            .Where(t => t.TransactionType == TransactionType.OrderPayment
                        && t.Status == TransactionStatus.Pending
                        && t.CreatedAt < timeoutThreshold
                        && t.OrderId != null
                        && t.Order != null
                        && t.Order.IsDeleted == false
                        && t.Order.Status == OrderStatus.PendingPayment
                        && t.Order.PaymentStatus == PaymentStatus.UnPaid)
            .Select(t => t.OrderId!.Value)
            .Distinct()
            .ToListAsync();

        if (!orderIdsToCancel.Any())
        {
            return;
        }

        var orders = await _dbContext.Orders
            .AsSplitQuery()
            .Include(o => o.SellerOrders)
            .Include(o => o.OrderDetails)
            .Where(o => orderIdsToCancel.Contains(o.Id) && o.IsDeleted == false)
            .ToListAsync();

        if (!orders.Any())
        {
            return;
        }

        var productIds = orders
            .SelectMany(o => o.OrderDetails.Select(d => d.ProductId))
            .Distinct()
            .ToList();

        var productsById = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var pendingTransactions = await _dbContext.Transactions
            .Where(t => t.TransactionType == TransactionType.OrderPayment
                        && t.Status == TransactionStatus.Pending
                        && t.OrderId != null
                        && orderIdsToCancel.Contains(t.OrderId.Value))
            .ToListAsync();

        foreach (var trans in pendingTransactions)
        {
            trans.Status = TransactionStatus.Expired;
            trans.Description = "Hệ thống tự động hủy do quá 10 phút không thanh toán.";
            trans.UpdatedAt = now;
        }

        foreach (var order in orders)
        {
            order.Status = OrderStatus.Cancelled;
            order.PaymentStatus = PaymentStatus.Failed;
            order.CancelReason = "Payment timeout (>= 10 minutes)";
            order.UpdatedAt = now;

            foreach (var sellerOrder in order.SellerOrders)
            {
                sellerOrder.Status = OrderStatus.Cancelled;
                sellerOrder.CancelReason = "Payment timeout (>= 10 minutes)";
                sellerOrder.UpdatedAt = now;
            }

            foreach (var detail in order.OrderDetails)
            {
                if (productsById.TryGetValue(detail.ProductId, out var product) &&
                    product.Status == ProductStatus.OnHold)
                {
                    product.Status = ProductStatus.Available;
                    product.UpdatedAt = now;
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Đã auto-cancel {Count} đơn hàng quá hạn thanh toán.", orders.Count);
    }
}

