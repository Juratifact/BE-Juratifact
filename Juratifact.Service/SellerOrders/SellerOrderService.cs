using System.Security.Claims;
using System.Linq.Expressions;
using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.SellerOrders;

public class SellerOrderService : ISellerOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public SellerOrderService(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<Response.PageResult<SellerOrderResponse>> GetMySellerOrders(
        OrderStatus? status,
        int pageSize,
        int pageIndex)
    {
        var userId = GetCurrentUserId();

        var query = _dbContext.SellerOrders
            .Where(so => so.SellerId == userId && so.IsDeleted == false);

        if (status.HasValue)
        {
            query = query.Where(so => so.Status == status.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(so => so.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(SellerOrderProjection)
            .ToListAsync();

        return new Response.PageResult<SellerOrderResponse>
        {
            Items = items,
            TotalItems = totalItems,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<List<SellerOrderResponse>> GetSellerOrdersByParentOrderId(Guid orderId)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsInRole("Admin");

        var orderExists = await _dbContext.Orders
            .AnyAsync(o => o.Id == orderId &&
                           o.IsDeleted == false &&
                           (isAdmin || o.UserId == userId));

        if (!orderExists)
        {
            throw new UnauthorizedAccessException("Order not found or you do not have permission to access it.");
        }

        return await _dbContext.SellerOrders
            .Where(so => so.OrderId == orderId && so.IsDeleted == false)
            .OrderBy(so => so.Code)
            .Select(SellerOrderProjection)
            .ToListAsync();
    }

    public async Task<SellerOrderResponse> GetSellerOrderById(Guid sellerOrderId)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsInRole("Admin");
        var isShipper = IsInRole("Shipper");

        var sellerOrder = await _dbContext.SellerOrders
            .Where(so => so.Id == sellerOrderId && so.IsDeleted == false)
            .Where(so => isAdmin ||
                         so.SellerId == userId ||
                         so.Order.UserId == userId ||
                         so.ShipperId == userId ||
                         (isShipper &&
                          so.Order.PaymentStatus == PaymentStatus.Paid &&
                          so.Status == OrderStatus.Paid &&
                          so.ShipperId == null))
            .Select(SellerOrderProjection)
            .FirstOrDefaultAsync();

        if (sellerOrder == null)
        {
            throw new UnauthorizedAccessException("Seller order not found or you do not have permission to access it.");
        }

        return sellerOrder;
    }

    public async Task<List<SellerOrderTransactionResponse>> GetSellerOrderTransactions(Guid sellerOrderId)
    {
        var userId = GetCurrentUserId();
        var isAdmin = IsInRole("Admin");

        var canAccess = await _dbContext.SellerOrders
            .AnyAsync(so => so.Id == sellerOrderId &&
                            so.IsDeleted == false &&
                            (isAdmin || so.SellerId == userId));

        if (!canAccess)
        {
            throw new UnauthorizedAccessException("Seller order not found or you do not have permission to access its transactions.");
        }

        return await _dbContext.Transactions
            .Where(t => t.SellerOrderId == sellerOrderId && t.IsDeleted == false)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new SellerOrderTransactionResponse
            {
                Id = t.Id,
                OrderId = t.OrderId,
                SellerOrderId = t.SellerOrderId,
                WalletId = t.WalletId,
                Amount = t.Amount,
                FeeAmount = t.FeeAmount,
                ReferenceCode = t.ReferenceCode,
                Description = t.Description,
                TransactionType = t.TransactionType,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();
    }

    private static readonly Expression<Func<SellerOrder, SellerOrderResponse>> SellerOrderProjection = so =>
        new SellerOrderResponse
        {
            Id = so.Id,
            Code = so.Code,
            ParentOrderId = so.OrderId,
            ParentOrderCode = so.Order.Name,
            BuyerId = so.Order.UserId,
            BuyerName = so.Order.User.FullName,
            BuyerPhone = so.Order.User.PhoneNumber,
            ShippingAddress = so.Order.ShippingAddress,
            ShippingVietMapRefId = so.Order.VietMapRefId,
            ShippingLatitude = so.Order.ShippingLatitude,
            ShippingLongitude = so.Order.ShippingLongitude,
            SellerId = so.SellerId,
            SellerName = so.Seller.FullName,
            SellerPhone = so.Seller.PhoneNumber,
            SellerAddress = so.Seller.VietMapDisplay ?? so.Seller.Address,
            SellerVietMapRefId = so.Seller.VietMapRefId,
            SellerVietMapDisplay = so.Seller.VietMapDisplay,
            SellerLatitude = so.Seller.Latitude,
            SellerLongitude = so.Seller.Longitude,
            ShipperId = so.ShipperId,
            ShipperName = so.Shipper != null ? so.Shipper.FullName : null,
            SubtotalPrice = so.SubtotalPrice,
            ShippingFee = so.ShippingFee,
            DiscountAmount = so.DiscountAmount,
            TotalPrice = so.TotalPrice,
            PlatformFee = so.PlatformFee,
            SellerReceivableAmount = so.SellerReceivableAmount,
            Status = so.Status,
            PaymentStatus = so.Order.PaymentStatus,
            PaymentMethod = so.Order.PaymentMethod,
            ShipperPod1Url = so.ShipperPod1Url,
            ShipperPod2Url = so.ShipperPod2Url,
            PickupAt = so.PickupAt,
            DeliveryAt = so.DeliveryAt,
            CancelReason = so.CancelReason,
            CreatedAt = so.CreatedAt,
            UpdatedAt = so.UpdatedAt,
            Items = so.OrderDetails.Select(od => new SellerOrderItemResponse
            {
                ProductId = od.ProductId,
                ProductTitle = od.Product.Title,
                Condition = od.Product.Condition,
                UnitPrice = od.Price,
                Quantity = od.Quantity,
                TotalPrice = od.Price * od.Quantity,
                ImageUrl = od.Product.ProductMedias
                    .Where(m => !string.IsNullOrEmpty(m.ImageUrl))
                    .Select(m => m.ImageUrl)
                    .ToList()
            }).ToList()
        };

    private Guid GetCurrentUserId()
    {
        var userId = _httpContext.HttpContext?.User.Claims
            .FirstOrDefault(x => x.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        return userIdGuid;
    }

    private bool IsInRole(string role)
    {
        return _httpContext.HttpContext?.User.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "Role") &&
            string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase)) == true;
    }
}
