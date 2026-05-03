using Quartz;
using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Juratifact.Service.BackgroundJobService;

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
    _logger.LogInformation("Đang chạy CancelOrderJob: Quét các đơn hàng hết hạn thanh toán...");
    
    // Ngưỡng thời gian: 10 phút trước
    var timeoutThreshold = DateTimeOffset.UtcNow.AddMinutes(-10);

    // Tìm các giao dịch OrderPayment đang Pending và quá 10 phút
    var pendingTransactions = await _dbContext.Transactions
        .Include(t => t.Order)
            .ThenInclude(o => o.OrderDetails) 
        .Where(t => t.TransactionType == TransactionType.OrderPayment 
                    && t.Status == TransactionStatus.Pending 
                    && t.CreatedAt < timeoutThreshold)
        .ToListAsync();

    if (pendingTransactions.Any())
    {
        // 1. Lấy tất cả Product ID cần giải phóng để load một lần (tối ưu hiệu năng)
        var productIds = pendingTransactions
            .SelectMany(t => t.Order?.OrderDetails?.Select(d => d.ProductId) ?? Enumerable.Empty<Guid>())
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var trans in pendingTransactions)
        {
            // Hủy giao dịch
            trans.Status = TransactionStatus.Failed;
            trans.Description = "Hệ thống tự động hủy do quá 10 phút không thanh toán.";
            
            if (trans.Order != null)
            {
                trans.Order.Status = OrderStatus.Cancelled;
                trans.Order.UpdatedAt = DateTimeOffset.UtcNow;

                // 2. Chỉ cần reset trạng thái về Available (Không cần đụng đến StockQuantity)
                foreach (var detail in trans.Order.OrderDetails)
                {
                    var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                    if (product != null)
                    {
                        product.Status = ProductStatus.Available; 
                    }
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Đã hủy {Count} đơn hàng và giải phóng sản phẩm về Available.", pendingTransactions.Count);
    }
}
}