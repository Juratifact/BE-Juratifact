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
        // Process settlement per seller for a multi-seller order.
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Load order with details and product info (so we can get SellerId)
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
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

            // 2. Group order details by seller
            var details = order.OrderDetails ?? Enumerable.Empty<OrderDetail>();

            var bySeller = details
                .Where(d => d.Product != null)
                .GroupBy(d => d.Product.SellerId)
                .ToList();

            if (!bySeller.Any())
            {
                _logger.LogWarning("ProcessSettlement: order {OrderId} has no order details to settle", orderId);
                return false;
            }

            const decimal commissionRate = 0.05m;
            var transactionsToAdd = new List<Transaction>();

            foreach (var group in bySeller)
            {
                var sellerId = group.Key;
                // Sum prices for this seller
                var sellerGross = group.Sum(d => d.Price);
                var commission = Math.Round(sellerGross * commissionRate, 2);
                var sellerNet = sellerGross - commission;

                // Get or create seller wallet
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == sellerId);
                if (wallet == null)
                {
                    wallet = new Wallet
                    {
                        Id = Guid.NewGuid(),
                        UserId = sellerId,
                        Balance = 0m,
                        PendingBalance = 0m,
                        IsDeleted = false,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _context.Wallets.Add(wallet);
                }

                // Credit seller
                wallet.Balance += sellerNet;
                wallet.UpdatedAt = DateTimeOffset.UtcNow;

                // Create transactions: seller settlement (credit to wallet) and commission record
                var sellerTx = new Transaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    WalletId = wallet.Id,
                    Amount = sellerNet,
                    TransactionType = TransactionType.SellerSettlement,
                    Status = TransactionStatus.Success,
                    ReferenceCode = $"SETTLE-{Guid.NewGuid():N}",
                    Description = $"Seller settlement for order {orderId}",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var commissionTx = new Transaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Amount = commission,
                    TransactionType = TransactionType.CommisionDeduction,
                    Status = TransactionStatus.Success,
                    ReferenceCode = $"COMM-{Guid.NewGuid():N}",
                    Description = $"Platform commission for seller {sellerId} on order {orderId}",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                transactionsToAdd.Add(sellerTx);
                transactionsToAdd.Add(commissionTx);
            }

            // 3. Mark order completed and payment as settled
            order.Status = OrderStatus.Completed;
            order.PaymentStatus = PaymentStatus.Settled;
            order.UpdatedAt = DateTimeOffset.UtcNow;

            // 4. Persist
            _context.Transactions.AddRange(transactionsToAdd);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing settlement for order {OrderId}", orderId);
            return false;
        }
    }
}