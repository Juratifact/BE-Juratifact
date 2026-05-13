using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.Sepay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Juratifact.Service.SettlementService;

public class SettlementService: ISettlementService
{
    private readonly AppDbContext _context;
    private readonly ISepayService _sePayService; // Service gọi API SePay
    private readonly ILogger _logger;

    public SettlementService(AppDbContext context, ISepayService sePayService, ILogger<SettlementService> logger)
    {
        _context = context;
        _sePayService = sePayService;
        _logger = logger;
    }
    
    public async Task<bool> ProcessSettlementAsync(Guid orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var sellerOrder = await _context.SellerOrders
                .Include(so => so.OrderDetails)
                .Include(so => so.Order)
                    .ThenInclude(o => o.SellerOrders)
                .FirstOrDefaultAsync(so => so.Id == orderId);

            if (sellerOrder != null)
            {
                var settledSingle = await SettleSellerOrderAsync(sellerOrder);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return settledSingle;
            }

            var order = await _context.Orders
                .Include(o => o.SellerOrders)
                    .ThenInclude(so => so.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("ProcessSettlement: order {OrderId} not found", orderId);
                return false;
            }

            if (order.Status == OrderStatus.Completed)
            {
                _logger.LogInformation("ProcessSettlement: order {OrderId} already completed", orderId);
                return false;
            }

            // Only settle if payment has been made
            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                _logger.LogWarning("ProcessSettlement: order {OrderId} payment status is not Paid (actual: {PaymentStatus})", orderId, order.PaymentStatus);
                return false;
            }

            var sellerOrders = order.SellerOrders
                .Where(so => so.Status == OrderStatus.Delivered)
                .ToList();

            if (!sellerOrders.Any())
            {
                _logger.LogWarning("ProcessSettlement: order {OrderId} has no delivered seller orders to settle", orderId);
                return false;
            }

            var settledAny = false;
            foreach (var item in sellerOrders)
            {
                settledAny |= await SettleSellerOrderAsync(item);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return settledAny;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing settlement for order {OrderId}", orderId);
            return false;
        }
    }

    private async Task<bool> SettleSellerOrderAsync(SellerOrder sellerOrder)
    {
        if (sellerOrder.Order.PaymentStatus != PaymentStatus.Paid)
        {
            _logger.LogWarning("SettleSellerOrder: parent order {OrderId} payment status is not Paid", sellerOrder.OrderId);
            return false;
        }

        if (sellerOrder.Status == OrderStatus.Completed)
        {
            _logger.LogInformation("SettleSellerOrder: seller order {SellerOrderId} already completed", sellerOrder.Id);
            return false;
        }

        if (sellerOrder.Status != OrderStatus.Delivered)
        {
            _logger.LogWarning("SettleSellerOrder: seller order {SellerOrderId} is not Delivered", sellerOrder.Id);
            return false;
        }

        var alreadySettled = await _context.Transactions.AnyAsync(t =>
            t.SellerOrderId == sellerOrder.Id &&
            t.TransactionType == TransactionType.SellerSettlement &&
            t.Status == TransactionStatus.Success);

        if (alreadySettled)
        {
            sellerOrder.Status = OrderStatus.Completed;
            sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
            UpdateParentSettlementStatus(sellerOrder.Order);
            return false;
        }

        var sellerGross = sellerOrder.SubtotalPrice > 0
            ? sellerOrder.SubtotalPrice
            : sellerOrder.OrderDetails.Sum(d => d.Price * d.Quantity);
        var commission = sellerOrder.PlatformFee > 0
            ? sellerOrder.PlatformFee
            : Math.Round(sellerGross * 0.05m, 2);
        var sellerNet = sellerOrder.SellerReceivableAmount > 0
            ? sellerOrder.SellerReceivableAmount
            : sellerGross - commission;

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == sellerOrder.SellerId);
        if (wallet == null)
        {
            wallet = new Repository.Entity.Wallet()
            {
                Id = Guid.NewGuid(),
                UserId = sellerOrder.SellerId,
                Balance = 0m,
                PendingBalance = 0m,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.Wallets.Add(wallet);
        }

        wallet.Balance += sellerNet;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = sellerOrder.OrderId,
            SellerOrderId = sellerOrder.Id,
            WalletId = wallet.Id,
            Amount = sellerNet,
            FeeAmount = commission,
            TransactionType = TransactionType.SellerSettlement,
            Status = TransactionStatus.Success,
            ReferenceCode = $"SETTLE-{Guid.NewGuid():N}",
            Description = $"Seller settlement for seller order {sellerOrder.Id}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = sellerOrder.OrderId,
            SellerOrderId = sellerOrder.Id,
            Amount = commission,
            TransactionType = TransactionType.CommisionDeduction,
            Status = TransactionStatus.Success,
            ReferenceCode = $"COMM-{Guid.NewGuid():N}",
            Description = $"Platform commission for seller order {sellerOrder.Id}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        sellerOrder.Status = OrderStatus.Completed;
        sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
        UpdateParentSettlementStatus(sellerOrder.Order);
        return true;
    }

    private static void UpdateParentSettlementStatus(Repository.Entity.Order order)
    {
        if (order.SellerOrders.All(so => so.Status is OrderStatus.Completed or OrderStatus.Cancelled))
        {
            order.Status = OrderStatus.Completed;
            order.PaymentStatus = PaymentStatus.Settled;
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
