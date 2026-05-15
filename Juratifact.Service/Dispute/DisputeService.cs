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
        var userIdGuid = GetCurrentUserId();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var sellerOrder = await _dbContext.SellerOrders
                .Include(so => so.Order)
                    .ThenInclude(o => o.SellerOrders)
                .FirstOrDefaultAsync(so => so.Id == request.SellerOrderId &&
                                           so.OrderId == orderId &&
                                           so.Order.UserId == userIdGuid &&
                                           so.IsDeleted == false &&
                                           so.Order.IsDeleted == false);

            if (sellerOrder == null)
            {
                throw new Exception("Seller order not found or you do not have permission to access it.");
            }

            if (sellerOrder.Status != OrderStatus.Delivered)
            {
                throw new Exception("Disputes can only be opened for successfully delivered seller orders.");
            }

            var isAlreadyDisputed = await _dbContext.Disputes.AnyAsync(d =>
                d.SellerOrderId == sellerOrder.Id &&
                d.Status != DisputeStatus.Resolved &&
                d.IsDeleted == false);

            if (isAlreadyDisputed)
            {
                throw new Exception("A dispute has already been submitted for this seller order and is pending resolution.");
            }

            sellerOrder.Status = OrderStatus.Disputed;
            sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
            UpdateParentOrderStatus(sellerOrder.Order);

            var dispute = new Repository.Entity.Dispute
            {
                OrderId = sellerOrder.OrderId,
                SellerOrderId = sellerOrder.Id,
                BuyerId = userIdGuid,
                Reason = request.Reason,
                Status = DisputeStatus.Open,
                Resolution = DisputeResolution.None,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Disputes.Add(dispute);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Dispute submitted successfully. The seller order has been frozen and funds will be held until an administrator makes a decision.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Base.Response.PageResult<Response.DisputeResponse>> GetMyDispute(int pageSize, int pageIndex)
    {
        var userIdGuid = GetCurrentUserId();

        var query = _dbContext.Disputes
            .Where(x => x.BuyerId == userIdGuid && x.IsDeleted == false)
            .OrderByDescending(x => x.CreatedAt);

        var totalItems = await query.CountAsync();

        var listResult = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.DisputeResponse
            {
                DisputeId = x.Id,
                OrderId = x.OrderId,
                SellerOrderId = x.SellerOrderId,
                BuyerId = x.BuyerId,
                Reason = x.Reason,
                Status = x.Status,
                Resolution = x.Resolution,
                AdminNote = x.AdminNote,
                ResolvedByAdminId = x.ResolvedByAdminId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.DisputeResponse>
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<string> ResolveDispute(Guid disputeId, Request.ResolveDisputeRequest request)
    {
        var userIdGuid = GetCurrentUserId();

        var dispute = await _dbContext.Disputes
            .Include(d => d.Order)
                .ThenInclude(o => o!.SellerOrders)
            .Include(d => d.SellerOrder)
                .ThenInclude(so => so!.OrderDetails)
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
        var sellerOrder = dispute.SellerOrder;

        if (sellerOrder == null)
        {
            throw new InvalidOperationException("This dispute is not linked to a seller order.");
        }

        if (request.Result == DisputeResolution.RefundBuyer)
        {
            await RefundSellerOrderAsync(dispute, sellerOrder, order);
        }
        else if (request.Result == DisputeResolution.PaySeller)
        {
            sellerOrder.Status = OrderStatus.Delivered;
            sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
            UpdateParentOrderStatus(order);
            await _dbContext.SaveChangesAsync();

            var isSettled = await _settlementService.ProcessSettlementAsync(sellerOrder.Id);
            if (!isSettled)
            {
                throw new InvalidOperationException("An error occurred while processing settlement for the seller order.");
            }
        }
        else if (request.Result == DisputeResolution.PartialRefund)
        {
            throw new NotImplementedException("The partial refund feature is currently under development.");
        }
        else
        {
            throw new ArgumentException("Invalid resolution result.");
        }

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
        var userIdGuid = GetCurrentUserId();

        var dispute = await _dbContext.Disputes
            .Include(x => x.Order)
                .ThenInclude(o => o!.SellerOrders)
            .Include(x => x.SellerOrder)
            .FirstOrDefaultAsync(x => x.Id == disputeId && x.IsDeleted == false);

        if (dispute == null)
        {
            throw new Exception("Dispute not found.");
        }

        if (dispute.Order!.UserId != userIdGuid)
        {
            throw new Exception("You do not have permission to cancel this dispute.");
        }

        if (dispute.Status == DisputeStatus.Resolved)
        {
            throw new Exception("Cannot cancel a dispute that has already been resolved.");
        }

        if (dispute.SellerOrder == null)
        {
            throw new InvalidOperationException("This dispute is not linked to a seller order.");
        }

        dispute.Status = DisputeStatus.Resolved;
        dispute.AdminNote = "Dispute withdrawn by the buyer.";
        dispute.Resolution = DisputeResolution.PaySeller;
        dispute.UpdatedAt = DateTimeOffset.UtcNow;

        dispute.SellerOrder.Status = OrderStatus.Delivered;
        dispute.SellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
        UpdateParentOrderStatus(dispute.Order);
        await _dbContext.SaveChangesAsync();

        var isSettlementSuccess = await _settlementService.ProcessSettlementAsync(dispute.SellerOrder.Id);

        if (!isSettlementSuccess)
        {
            throw new Exception("Failed to cancel dispute. Could not process settlement for the seller order.");
        }

        await _dbContext.SaveChangesAsync();

        return "Dispute cancelled successfully. The seller order is now completed and seller has been paid.";
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

        var totalItems = await query.CountAsync();

        var listResult = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.DisputeResponse
            {
                DisputeId = x.Id,
                OrderId = x.OrderId,
                SellerOrderId = x.SellerOrderId,
                BuyerId = x.BuyerId,
                Reason = x.Reason,
                Status = x.Status,
                Resolution = x.Resolution,
                AdminNote = x.AdminNote,
                ResolvedByAdminId = x.ResolvedByAdminId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.DisputeResponse>
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<string> AssignDispute(Guid disputeId, Request.AssignDisputeRequest request)
    {
        var userIdGuid = GetCurrentUserId();
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

    private async Task RefundSellerOrderAsync(Repository.Entity.Dispute dispute, SellerOrder sellerOrder, Repository.Entity.Order order)
    {
        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            throw new InvalidOperationException("Only paid orders can be refunded.");
        }

        var buyerWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == dispute.BuyerId);
        if (buyerWallet == null)
        {
            throw new InvalidOperationException("Buyer's wallet not found for refund.");
        }

        var totalRefund = sellerOrder.OrderDetails.Sum(d => d.Price * d.Quantity);
        buyerWallet.Balance += totalRefund;
        buyerWallet.UpdatedAt = DateTimeOffset.UtcNow;

        var refundRefCode =
            $"RF-DISP-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{sellerOrder.Id.ToString()[..8].ToUpper()}";

        _dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            SellerOrderId = sellerOrder.Id,
            WalletId = buyerWallet.Id,
            Amount = totalRefund,
            TransactionType = TransactionType.Refund,
            Status = TransactionStatus.Success,
            ReferenceCode = refundRefCode,
            Description = $"Refund for dispute {dispute.Id} on seller order {sellerOrder.Id}. Decided by Admin.",
            CreatedAt = DateTimeOffset.UtcNow
        });

        sellerOrder.Status = OrderStatus.Cancelled;
        sellerOrder.CancelReason = $"Refunded after dispute {dispute.Id}";
        sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var detail in sellerOrder.OrderDetails)
        {
            detail.Product.Status = ProductStatus.Available;
            detail.Product.UpdatedAt = DateTimeOffset.UtcNow;
        }

        UpdateParentOrderStatus(order);
    }

    private Guid GetCurrentUserId()
    {
        var userId = _httpContext.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        return userIdGuid;
    }

    private static void UpdateParentOrderStatus(Repository.Entity.Order order)
    {
        var sellerOrders = order.SellerOrders
            .Where(so => so.IsDeleted == false)
            .ToList();

        if (sellerOrders.Count == 0)
        {
            return;
        }

        if (sellerOrders.All(so => so.Status == OrderStatus.Cancelled))
        {
            order.Status = OrderStatus.Cancelled;
            order.PaymentStatus = PaymentStatus.Refunded;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (sellerOrders.All(so => so.Status is OrderStatus.Completed or OrderStatus.Cancelled))
        {
            order.Status = OrderStatus.Completed;
            order.PaymentStatus = PaymentStatus.Settled;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (sellerOrders.All(so => so.Status is OrderStatus.Disputed or OrderStatus.Completed or OrderStatus.Cancelled))
        {
            order.Status = OrderStatus.Disputed;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (sellerOrders.Any(so => so.Status == OrderStatus.Shipping))
        {
            order.Status = OrderStatus.Shipping;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (sellerOrders.Any(so => so.Status == OrderStatus.Assigned))
        {
            order.Status = OrderStatus.Assigned;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (sellerOrders.Any(so => so.Status == OrderStatus.Delivered))
        {
            order.Status = OrderStatus.Delivered;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        if (sellerOrders.All(so => so.Status == OrderStatus.Paid))
        {
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
