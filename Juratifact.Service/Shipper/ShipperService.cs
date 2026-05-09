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
        var query = _dbContext.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.ShipperId == null);

        var selected = query.Select(o => new Response.ShipperResponse()
        {
            OrderId =  o.Id,
            AddressSeller = o.OrderDetails.Select(od => od.Product.Seller.Address).FirstOrDefault(),
            AddressBuyer = o.User.Address,
            TotalPrice = o.TotalPrice
        });
        var result = await selected.ToListAsync();
        return result;
    }

    public async Task<string> AcceptOrder(Guid orderId, Guid shipperId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId 
                                      && o.PaymentStatus == PaymentStatus.Paid 
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
        var query = await _dbContext.Orders
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
            UserId = query.UserId,
            Type = NotificationType.OrderShipped,
            Data = query.Name, 
        });
        
        return  "Successfully shipped order";
    }

    public async Task<string> ConfirmDelivery(Guid orderId, Guid shipperId, IFormFile pod2Image)
    {
        var query = await _dbContext.Orders
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

        await _dbContext.SaveChangesAsync();
        
        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest() {
            UserId = query.UserId,
            Type = NotificationType.OrderDelivered,
            Data = query.Name, 
        });
        
        return  "Successfully";
    }

    public async Task<List<Response.ShipperActiveOrderResponse>> GetMyOrdersShipper(Guid shipperId)
    {
        var query = await _dbContext.Orders
            .Where(o => o.ShipperId == shipperId 
                        && o.Status != OrderStatus.Delivered 
                        && o.Status != OrderStatus.Cancelled)
            .Select(o => new Response.ShipperActiveOrderResponse()
            {
                OrderId       = o.Id,
                Name          = o.Name,
                Status        = o.Status,

                TotalPrice    = o.TotalPrice,
                ShippingFee   = o.ShippingPee,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,

                ShippingAddress = o.ShippingAddress,

                CustomerName  = o.User.FullName,
                CustomerPhone = o.User.PhoneNumber,

                PickupAt   = o.PickupAt,
                DeliveryAt = o.DeliveryAt,
                CreatedAt  = o.CreatedAt,
                ExpiresAt  = o.ExpiresAt,

                ShipperPod1Url = o.ShipperPod1Url,
                ShipperPod2Url = o.ShipperPod2Url,

                Items = o.OrderDetails.Select(od => new Response.OrderDetailDto()
                {
                    ProductId   = od.ProductId,
                    ProductName = od.Product.Title,
                    Price       = od.Price,
                }).ToList()
            })
            .ToListAsync();

        return query;
    }

    public async Task<Response.ShipperActiveOrderResponse?> GetMyOrdersShipperByOrderId(Guid shipperId, Guid orderId)
    {
        var query = await _dbContext.Orders
            .Where(o => o.ShipperId == shipperId 
                        && o.Id == orderId
                        && o.Status != OrderStatus.Delivered 
                        && o.Status != OrderStatus.Cancelled)
            .Select(o => new Response.ShipperActiveOrderResponse()
            {
                OrderId       = o.Id,
                Name          = o.Name,
                Status        = o.Status,

                TotalPrice    = o.TotalPrice,
                ShippingFee   = o.ShippingPee,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,

                ShippingAddress = o.ShippingAddress,

                CustomerName  = o.User.FullName,
                CustomerPhone = o.User.PhoneNumber,

                PickupAt   = o.PickupAt,
                DeliveryAt = o.DeliveryAt,
                CreatedAt  = o.CreatedAt,
                ExpiresAt  = o.ExpiresAt,

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