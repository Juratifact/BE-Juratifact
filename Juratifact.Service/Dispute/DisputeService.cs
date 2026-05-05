using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.Sepay;
using Juratifact.Service.SettlementService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Dispute;

public class DisputeService : IDisputeService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ISepayService _sepayService;
    private readonly ISettlementService _settlementService;

    public DisputeService(AppDbContext dbContext, IHttpContextAccessor httpContext, ISepayService sepayService,
        ISettlementService settlementService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _sepayService = sepayService;
        _settlementService = settlementService;
    }

    public async Task<string> CreateDispute(Guid orderId, Request.CreateDisputeRequest request)
    {
        // 1. Get UserId safely
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var userIdGuid = Guid.Parse(userId);

        // Begin transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 2. Load Order
            var order = await _dbContext.Orders.FirstOrDefaultAsync(x => x.Id == orderId &&
                                                                         x.UserId == userIdGuid
                                                                         && x.IsDeleted == false);
            if (order == null)
            {
                throw new Exception("Order not found or you do not have permission to access it.");
            }

            // 3. Validate Status: Only allow dispute if order is delivered
            if (order.Status != OrderStatus.Delivered)
            {
                throw new Exception("Disputes can only be opened for successfully delivered orders.");
            }

            // Anti-Spam: Check if a dispute already exists for this order
            var isAlreadyDisputed = await _dbContext.Disputes.AnyAsync(d => d.OrderId == orderId);
            if (isAlreadyDisputed)
            {
                throw new Exception("A dispute has already been submitted for this order and is pending resolution.");
            }

            // 4. Update Status = Disputed (FREEZE THE ORDER)
            order.Status = OrderStatus.Disputed;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            // 5. Create Dispute Record
            var dispute = new Repository.Entity.Dispute()
            {
                OrderId = order.Id,
                BuyerId = userIdGuid,
                Reason = request.Reason,
                Status = DisputeStatus.Open, // <--- Đặt trạng thái là Open
                Resolution = DisputeResolution.None, // <--- BỔ SUNG: Khởi tạo kết quả phân xử là None
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.Disputes.Add(dispute);
            // 6. Save and Commit
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return
                "Dispute submitted successfully. The order has been frozen and funds will be held until an administrator makes a decision.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> ResolveDispute(Guid disputeId, Request.ResolveDisputeRequest request)
    {
        // 1. Get UserId safely
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var userIdGuid = Guid.Parse(userId);

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 2. Load Dispute along with Order, OrderDetails, and Products
            var dispute = await _dbContext.Disputes
                .Include(d => d.Order)
                .ThenInclude(o => o!.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(x => x.Id == disputeId);

            if (dispute == null)
            {
                throw new Exception("Dispute not found.");
            }

            if (dispute.Status == DisputeStatus.Resolved) 
            {
                throw new Exception("This dispute has already been resolved.");
            }

            var order = dispute.Order!;

            // 3. RESOLUTION LOGIC
            if (request.Result == DisputeResolution.RefundBuyer)
            {
                // === SCENARIO 1: BUYER WINS (RETURN & REFUND) ===

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTimeOffset.UtcNow;

                // Process refund to the Buyer's wallet
                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    var buyerWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == dispute.BuyerId);
                    if (buyerWallet == null)
                        throw new Exception("Buyer's wallet not found for refund.");

                    var totalRefund = order.OrderDetails.Sum(d => d.Price);
                    buyerWallet.Balance += totalRefund;
                    buyerWallet.UpdatedAt = DateTimeOffset.UtcNow;

                    // Log the Refund Transaction
                    string refundRefCode =
                        $"RF-DISP-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{order.Id.ToString().Substring(0, 8).ToUpper()}";
                    var refundTx = new Transaction()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        WalletId = buyerWallet.Id,
                        Amount = totalRefund,
                        TransactionType = TransactionType.Refund,
                        Status = TransactionStatus.Success,
                        ReferenceCode = refundRefCode,
                        Description = $"Refund for dispute {dispute.Id}. Decided by Admin.",
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _dbContext.Transactions.Add(refundTx);

                    order.PaymentStatus = PaymentStatus.Refunded;
                }

                // Release product status back to Available for resale
                foreach (var detail in order.OrderDetails)
                {
                    detail.Product.Status = ProductStatus.Available;
                    detail.Product.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }
            else if (request.Result == DisputeResolution.PaySeller)
            {
                // === SCENARIO 2: SELLER WINS (REJECT DISPUTE) ===

                order.Status = OrderStatus.Completed;
                order.UpdatedAt = DateTimeOffset.UtcNow;

                // Call the Settlement service to transfer money to the Seller, just like ConfirmReceipt
                bool isSettled = await _settlementService.ProcessSettlementAsync(order.Id);
                if (!isSettled)
                {
                    throw new Exception("An error occurred while processing settlement for the seller.");
                }
            }
            else if (request.Result == DisputeResolution.PartialRefund)
            {
                // Placeholder for future partial refund scenario
                throw new NotImplementedException("The partial refund feature is currently under development.");
            }
            else
            {
                throw new Exception("Invalid resolution result.");
            }

            // 4. Close the Dispute
            dispute.Status = DisputeStatus.Resolved;
            dispute.Resolution = request.Result;
            dispute.AdminNote = request.AdminNote;
            dispute.ResolvedByAdminId = userIdGuid; // Track the Admin who resolved this
            dispute.UpdatedAt = DateTimeOffset.UtcNow;

            // 5. Save changes and commit transaction
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Dispute resolved successfully.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}