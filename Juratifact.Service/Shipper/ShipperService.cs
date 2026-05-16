using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Juratifact.Service.MediaService;
using Juratifact.Service.Notification;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Shipper;

public class ShipperService: IShipperService
{
    private readonly AppDbContext _dbContext;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;

    public ShipperService(AppDbContext dbContext, IMediaService mediaService,  INotificationService notificationService)
    {
        _dbContext = dbContext;
        _mediaService = mediaService;
        _notificationService = notificationService;
    }

    public async Task<List<Response.ShipperResponse>> GetListOrder()
    {
        var query = _dbContext.SellerOrders
            .Where(so => so.Order.PaymentStatus == PaymentStatus.Paid
                         && so.Status == OrderStatus.Paid
                         && so.ShipperId == null);

        var selected = query.Select(so => new Response.ShipperResponse()
        {
            OrderId = so.Id,
            SellerOrderId = so.Id,
            ParentOrderId = so.OrderId,
            Code = so.Code,
            Status = so.Status,
            PaymentStatus = so.Order.PaymentStatus,
            SellerId = so.SellerId,
            SellerName = so.Seller.FullName,
            AddressSeller = so.Seller.Address,
            AddressBuyer = so.Order.ShippingAddress,
            CustomerName = so.Order.User.FullName,
            CustomerPhone = so.Order.User.PhoneNumber,
            ShippingFee = so.ShippingFee,
            TotalPrice = so.TotalPrice,
            Items = so.OrderDetails.Select(od => new Response.OrderDetailDto()
            {
                ProductId = od.ProductId,
                ProductName = od.Product.Title,
                Price = od.Price * od.Quantity
            }).ToList()
        });
        var result = await selected.ToListAsync();
        return result;
    }

    public async Task<string> AcceptOrder(Guid orderId, Guid shipperId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var order = await _dbContext.SellerOrders
            .FirstOrDefaultAsync(o => o.Id == orderId
                                      && o.Order.PaymentStatus == PaymentStatus.Paid
                                      && o.Status == OrderStatus.Paid
                                      && o.ShipperId == null);

        
        if (order == null)
        {
            throw new ArgumentException("The order is unavailable or has already been claimed");
        }

        
        order.ShipperId = shipperId; 
        order.Status = OrderStatus.Assigned;
        order.UpdatedAt = DateTimeOffset.UtcNow;

  
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return "Successfully accepted order";
    }

    public async Task<string> ConfirmPickupOrder(Guid orderId, Guid shipperId, IFormFile pod1Image)
    {
        var query = await _dbContext.SellerOrders
            .Include(o => o.Order)
            .FirstOrDefaultAsync(o => o.Id == orderId &&
                                      o.ShipperId == shipperId && 
                                      o.Status == OrderStatus.Assigned);
        if (query == null)
        {
            throw new ArgumentException("Order not found or you have not rights");
        }
        
        var image = await _mediaService.UploadAsync(pod1Image);

        if (image == null)
        {
            throw new ArgumentException("Image not found");
        }
        query.Status = OrderStatus.Shipping;
        query.ShipperPod1Url = image;
        query.PickupAt = DateTimeOffset.UtcNow; // Sử dụng DateTimeOffset như dự án Juratifact đang dùng
        query.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        
        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest() {
            UserId = query.Order.UserId,
            Type = NotificationType.OrderShipped,
            Data = query.Code, 
        });
        
        return  "Successfully shipped order";
    }

    public async Task<string> ConfirmDelivery(Guid orderId, Guid shipperId, IFormFile pod2Image)
    {
        var query = await _dbContext.SellerOrders
            .Include(o => o.Order)
            .FirstOrDefaultAsync(o => o.Id == orderId &&
                                      o.ShipperId == shipperId && 
                                      o.Status == OrderStatus.Shipping);
        if (query == null)
        {
            throw new ArgumentException("Order not found or you have not rights");
        }
        
        var image = await _mediaService.UploadAsync(pod2Image);

        if (image == null)
        {
            throw new ArgumentException("Image not found");
        }
        query.Status = OrderStatus.Delivered;
        query.ShipperPod2Url = image;
        query.DeliveryAt = DateTimeOffset.UtcNow;
        query.UpdatedAt = DateTimeOffset.UtcNow;

        var parentOrder = await _dbContext.Orders
            .Include(o => o.SellerOrders)
            .FirstOrDefaultAsync(o => o.Id == query.OrderId);

        if (parentOrder != null && parentOrder.SellerOrders.All(so => so.Status == OrderStatus.Delivered))
        {
            parentOrder.Status = OrderStatus.Delivered;
            parentOrder.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        
        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest() {
            UserId = query.Order.UserId,
            Type = NotificationType.OrderDelivered,
            Data = query.Code, 
        });
        
        return  "Successfully";
    }

    public async Task<Base.Response.PageResult<Response.ShipperActiveOrderResponse>> GetMyOrdersShipper(Guid shipperId, int pageSize, int pageIndex)
    {
        var query = _dbContext.SellerOrders
            .Where(o => o.ShipperId == shipperId 
                        && o.Status != OrderStatus.Delivered 
                        && o.Status != OrderStatus.Cancelled);

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new Response.ShipperActiveOrderResponse()
            {
                OrderId       = o.Id,
                ParentOrderId = o.OrderId,
                SellerId      = o.SellerId,
                SellerName    = o.Seller.FullName,
                SellerPhone = o.Seller.PhoneNumber,
                SellerAddress = o.Seller.Address,
                Name          = o.Code,
                Status        = o.Status,

                TotalPrice    = o.TotalPrice,
                ShippingFee   = o.ShippingFee,
                PaymentMethod = o.Order.PaymentMethod,
                PaymentStatus = o.Order.PaymentStatus,

                ShippingAddress = o.Order.ShippingAddress,

                CustomerName  = o.Order.User.FullName,
                CustomerPhone = o.Order.User.PhoneNumber,

                PickupAt   = o.PickupAt,
                DeliveryAt = o.DeliveryAt,
                CreatedAt  = o.CreatedAt,
                ExpiresAt  = o.Order.ExpiresAt,
                

                ShipperPod1Url = o.ShipperPod1Url,
                ShipperPod2Url = o.ShipperPod2Url,

                Items = o.OrderDetails.Select(od => new Response.OrderDetailDto()
                {
                    ProductId   = od.ProductId,
                    ProductName = od.Product.Title,
                    Price       = od.Price,
                    ImageUrl = od.Product.ProductMedias.Where(m => !string.IsNullOrEmpty(m.ImageUrl)).Select(m => m.ImageUrl).ToList()
                }).ToList()
            })
            .ToListAsync();
        
        return new Base.Response.PageResult<Response.ShipperActiveOrderResponse>
        {
            Items = items,
            TotalItems = totalItems,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<Response.ShipperActiveOrderResponse?> GetMyOrdersShipperByOrderId(Guid shipperId, Guid orderId)
    {
        var query = await _dbContext.SellerOrders
            .Where(o => o.ShipperId == shipperId 
                        && o.Id == orderId
                        && o.Status != OrderStatus.Delivered 
                        && o.Status != OrderStatus.Cancelled)
            .Select(o => new Response.ShipperActiveOrderResponse()
            {
                OrderId       = o.Id,
                ParentOrderId = o.OrderId,
                SellerId      = o.SellerId,
                SellerName    = o.Seller.FullName,
                SellerAddress = o.Seller.Address,
                SellerPhone = o.Seller.PhoneNumber,
                Name          = o.Code,
                Status        = o.Status,

                TotalPrice    = o.TotalPrice,
                ShippingFee   = o.ShippingFee,
                PaymentMethod = o.Order.PaymentMethod,
                PaymentStatus = o.Order.PaymentStatus,

                ShippingAddress = o.Order.ShippingAddress,

                CustomerName  = o.Order.User.FullName,
                CustomerPhone = o.Order.User.PhoneNumber,

                PickupAt   = o.PickupAt,
                DeliveryAt = o.DeliveryAt,
                CreatedAt  = o.CreatedAt,
                ExpiresAt  = o.Order.ExpiresAt,

                ShipperPod1Url = o.ShipperPod1Url,
                ShipperPod2Url = o.ShipperPod2Url,

                Items = o.OrderDetails.Select(od => new Response.OrderDetailDto()
                {
                    ProductId   = od.ProductId,
                    ProductName = od.Product.Title,
                    Price       = od.Price,
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return query;
    }
}
