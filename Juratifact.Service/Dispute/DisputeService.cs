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

    public async Task<Base.Response.PageResult<Response.DisputeResponse>> GetMyDispute(int pageSize, int pageIndex)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("You are not logged in or your session has expired.");
        }

        var userIdGuid = Guid.Parse(userId);


        var query = _dbContext.Disputes
            .Where(x => x.BuyerId == userIdGuid && x.IsDeleted == false);

        query = query.OrderByDescending(x => x.CreatedAt);

        query = query.Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);

        var selected = query.Select(x => new Response.DisputeResponse()
        {
            DisputeId = x.Id,
            OrderId = x.OrderId,
            BuyerId = x.BuyerId,
            Reason = x.Reason,
            Status = x.Status,
            Resolution = x.Resolution,
            AdminNote = x.AdminNote,
            ResolvedByAdminId = x.ResolvedByAdminId,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        });
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.DisputeResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
    }

    public async Task<string> ResolveDispute(Guid disputeId, Request.ResolveDisputeRequest request)
    {
        // 1. Get UserId safely
        var userId = _httpContext.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var userIdGuid = Guid.Parse(userId);
        
        // 2. Load Dispute along with Order, OrderDetails, and Products
        var dispute = await _dbContext.Disputes
            .Include(d => d.Order)
            .ThenInclude(o => o!.OrderDetails!) // Dấu ! để bypass nullable warning
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(x => x.Id == disputeId && x.IsDeleted == false);

        if (dispute == null)
        {
            throw new KeyNotFoundException("Dispute not found.");
        }

        if (dispute.Status == DisputeStatus.Resolved)
        {
            throw new InvalidOperationException("This dispute has already been resolved.");
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
                {
                    throw new InvalidOperationException("Buyer's wallet not found for refund.");
                }

                // Sum up the price correctly
                var totalRefund = order.OrderDetails.Sum(d => d.Price);
                buyerWallet.Balance += totalRefund;
                buyerWallet.UpdatedAt = DateTimeOffset.UtcNow;

                // Log the Refund Transaction
                string refundRefCode =
                    $"RF-DISP-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{order.Id.ToString()[..8].ToUpper()}";
                var refundTx = new Transaction()
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    WalletId = buyerWallet.Id,
                    Amount = totalRefund,
                    TransactionType = TransactionType.Refund, // Make sure you have this enum value
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
            
            // Vì Settlement Service sẽ tự động cập nhật Order thành Completed sau khi tính tiền.

            bool isSettled = await _settlementService.ProcessSettlementAsync(order.Id);
            if (!isSettled)
            {
                throw new InvalidOperationException("An error occurred while processing settlement for the seller.");
            }
        }
        else if (request.Result == DisputeResolution.PartialRefund)
        {
            // Placeholder for future partial refund scenario
            throw new NotImplementedException("The partial refund feature is currently under development.");
        }
        else
        {
            throw new ArgumentException("Invalid resolution result.");
        }

        // 4. Close the Dispute
        dispute.Status = DisputeStatus.Resolved;
        dispute.Resolution = request.Result;
        dispute.AdminNote = request.AdminNote;
        dispute.ResolvedByAdminId = userIdGuid; 
        dispute.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _dbContext.SaveChangesAsync();

        return "Dispute resolved successfully.";
    }

    public async Task<string> CancelDispute(Guid disputeId)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);

        var dispute = await _dbContext.Disputes
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == disputeId &&
                                      x.IsDeleted == false);

        if (dispute == null)
        {
            throw new Exception("Dispute not found.");
        }

        // 3. Verify Ownership: Only the Buyer of the order can cancel the dispute
        if (dispute.Order.UserId != userIdGuid)
        {
            throw new Exception("You do not have permission to cancel this dispute.");
        }

        if (dispute.Status == DisputeStatus.Resolved)
        {
            throw new Exception("Cannot cancel a dispute that has already been resolved.");
        }

        // 5. Update Dispute Status
        dispute.Status = DisputeStatus.Resolved;
        dispute.AdminNote = "Dispute withdrawn by the buyer.";
        dispute.UpdatedAt = DateTimeOffset.UtcNow;

        // 6. Process Settlement
        bool isSettlementSuccess = await _settlementService.ProcessSettlementAsync(dispute.OrderId);

        if (!isSettlementSuccess)
        {
            throw new Exception("Failed to cancel dispute. Could not process settlement for the seller.");
        }

        // 7. Save changes (Dispute status update)
        await _dbContext.SaveChangesAsync();

        return "Dispute cancelled successfully. The order is now completed and seller has been paid.";
    }

    public async Task<Base.Response.PageResult<Response.DisputeResponse>> GetDisputes(DisputeStatus? status,
        int pageSize, int pageIndex)
    {
        var query = _dbContext.Disputes
            .Where(x => x.IsDeleted == false);


        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        query = query.Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);

        var selected = query.Select(x => new Response.DisputeResponse()
        {
            DisputeId = x.Id,
            OrderId = x.OrderId,
            BuyerId = x.BuyerId,
            Reason = x.Reason,
            Status = x.Status,
            Resolution = x.Resolution,
            AdminNote = x.AdminNote,
            ResolvedByAdminId = x.ResolvedByAdminId,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        });
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.DisputeResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
    }

    public async Task<string> AssignDispute(Guid disputeId, Request.AssignDisputeRequest request)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);

        var targetAdminId = request.AssignedAdminId ?? userIdGuid;

        var dispute = await _dbContext.Disputes
            .FirstOrDefaultAsync(x => x.Id == disputeId && x.IsDeleted == false);

        if (dispute == null)
        {
            throw new Exception("Dispute not found.");
        }

        if (dispute.Status != DisputeStatus.Open)
        {
            throw new Exception("This dispute has already been assigned or resolved by someone else.");
        }

        dispute.Status = DisputeStatus.InProgress;

        dispute.ResolvedByAdminId = targetAdminId;
        dispute.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return "Dispute assigned successfully.";
    }
}