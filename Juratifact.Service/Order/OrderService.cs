using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.Notification;
using Juratifact.Service.Sepay;
using Juratifact.Service.SettlementService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Order;

public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ISepayService _sepayService;
    private readonly ISettlementService _settlementService;
    private readonly INotificationService _notificationService;
    
    
    public OrderService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor, ISepayService sepayService, ISettlementService settlementService,  INotificationService notificationService)
    {
        _dbContext = dbContext;
        _httpContext = httpContextAccessor;
        _sepayService = sepayService;
        _settlementService = settlementService;
        _notificationService = notificationService;
    }
    
    public async Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CreateOrderRequest request)
{
    var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
    var userIdGuid = Guid.Parse(userId!);

    using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();
    try
    {
        // 1. Get/Create Cart
        var cart = await _dbContext.Carts.FirstOrDefaultAsync(x => x.UserId == userIdGuid) 
                   ?? new Repository.Entity.Cart { Id = Guid.NewGuid(), UserId = userIdGuid, CreatedAt = DateTimeOffset.UtcNow };
        
        if (cart.Id == Guid.Empty) _dbContext.Carts.Add(cart);

        // 2. Validate products
        var productIds = request.Products.Select(x => x.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

        if (products.Count != productIds.Count) throw new Exception("Một số sản phẩm không tồn tại.");

        decimal totalAmount = 0;
        
        // 3. Xử lý Trạng thái (Chuyển sang OnHold)
        foreach (var product in products)
        {
            // Kiểm tra xem sản phẩm có đang ở trạng thái Available không
            if (product.Status != ProductStatus.Available)
                throw new Exception($"Sản phẩm '{product.Title}' hiện không thể đặt hàng (Đang ở trạng thái: {product.Status}).");

            // Chuyển trạng thái sang OnHold để giữ chỗ
            product.Status = ProductStatus.OnHold;
            
            // Cộng dồn tiền
            totalAmount += product.Price;
        }

        if (totalAmount <= 0) throw new Exception("Tổng tiền không hợp lệ.");

        // 4. Tạo Order
        var newOrder = new Repository.Entity.Order()
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            Name = request.Name,
            TotalPrice = totalAmount,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.UnPaid,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Orders.Add(newOrder);

        // 5. Tạo OrderDetails và CartDetails
        foreach (var product in products)
        {
            _dbContext.Add(new OrderDetail()
            {
                Id = Guid.NewGuid(),
                OrderId = newOrder.Id,
                ProductId = product.Id,
                Price = product.Price, // Giá sản phẩm
                CreatedAt = DateTimeOffset.UtcNow,
            });
            
            _dbContext.Add(new CartDetail() { Id = Guid.NewGuid(), CartId = cart.Id, ProductId = product.Id, Quantity = 1, CreatedAt = DateTimeOffset.UtcNow });
        }

        // 6. Tạo Transaction
        var referenceCode = $"JURATIFACT{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        _dbContext.Transactions.Add(new Transaction()
        {
            Id = Guid.NewGuid(),
            OrderId = newOrder.Id,
            ReferenceCode = referenceCode,
            Amount = totalAmount,
            Status = TransactionStatus.Pending,
            TransactionType = TransactionType.OrderPayment,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _dbContext.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        
        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest() {
            UserId = userIdGuid,
            Type = NotificationType.OrderPlaced,
            Data = newOrder.Name, // Chỉ cần truyền cái ID đơn hàng thôi
        });

        var qrUrl = await _sepayService.GenerateQrCode(totalAmount, referenceCode);

        return new Response.CreateOrderResponse()
        {
            OrderId = newOrder.Id,
            QrUrl = qrUrl,
            ReferenceCode = referenceCode,
        };
    }
    catch (Exception)
    {
        await dbTransaction.RollbackAsync();
        throw;
    }
}

    public async Task<Response.GetOrderStatusResponse> GetStatusOrder(Guid id)
    {
        var query = _dbContext.Orders.Where(x => x.Id == id);

        var existingOrder = await query.FirstOrDefaultAsync();

        if (existingOrder == null)
        {
            throw new Exception("Order not found");
        }

        var response = new Response.GetOrderStatusResponse()
        {
            Status = existingOrder.Status,
        };
        
        return response;
    }

    public async Task<List<Response.GetAllOrderResponse>> GetAllOrders()
    {
        var query = _dbContext.Orders.Where(x => x.IsDeleted == false);

        var select = query.Select(x => new Response.GetAllOrderResponse()
        {
            OrderId = x.Id,
            Name = x.Name,
            Status = x.Status,
            PaymentStatus = x.PaymentStatus,
        });

        var result = await select.ToListAsync();
        return result;
    }
    
    public async Task<string> ConfirmReceipt(Guid orderId)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);

        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(x => x.Id == orderId &&
                                      x.UserId == userIdGuid &&
                                      x.IsDeleted == false);

        if (order == null)
        {
            throw new Exception("Order not found or you do not have permission to access it.");
        }

        // 3. Validate order status
        if (order.Status != OrderStatus.Delivered)
        {
            throw new Exception("Order is not delivered yet, cannot confirm receipt.");
        }

        // 4. Process settlement (This also updates the status to Completed internally)
        bool isSuccess = await _settlementService.ProcessSettlementAsync(orderId);

        if (!isSuccess)
        {
            throw new Exception("Failed to confirm receipt. Could not process settlement for the seller.");
        }
        
        await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest() {
            UserId = userIdGuid,
            Type = NotificationType.OrderShipped,
            Data = order.Name, // Chỉ cần truyền cái ID đơn hàng thôi
        });


        return "Receipt confirmed successfully.";
    }

    public async Task<string> CancelOrder(Guid orderId, Request.CancelOrderRequest request)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);
        
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(x => x.Id == orderId 
                                          && x.UserId == userIdGuid // BẢO MẬT: Phải là đơn của user này
                                          && x.IsDeleted == false);

            if (order == null)
            {
                throw new Exception("Order not found or you do not have permission to access it.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                throw new Exception("This order has already been cancelled.");
            }
            
            if ((int)order.Status >= (int)OrderStatus.Shipping)
            {
                throw new Exception("Order has already been handed over to the shipping carrier or is completed and cannot be cancelled.");
            }
            
            // 4. Update order status
            order.Status = OrderStatus.Cancelled;
            order.CancelReason = request.Reason;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            
            // 5. Process Refund (ONLY IF ALREADY PAID)
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                // Find buyer's wallet
                var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userIdGuid);
                if (wallet == null) 
                    throw new Exception("Buyer's wallet not found for refund.");

                // Calculate total refund amount
                var totalRefund = order.OrderDetails!.Sum(d => d.Price);

                // Credit the buyer's wallet
                wallet.Balance += totalRefund;
                wallet.UpdatedAt = DateTimeOffset.UtcNow;

                // Generate a unique Reference Code for the refund
                // Example format: RF-20260505162638-A1B2C3D4 (RF-Timestamp-ShortOrderId)
                string refundRefCode = $"RF-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{order.Id.ToString().Substring(0, 8).ToUpper()}";

                // Log the refund transaction
                var refundTx = new Transaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    WalletId = wallet.Id,
                    Amount = totalRefund,
                    TransactionType = TransactionType.Refund, 
                    Status = TransactionStatus.Success,
                    ReferenceCode = refundRefCode, // <--- BỔ SUNG DÒNG NÀY VÀO ĐÂY
                    Description = $"Refund for cancelled order {order.Id}. Reason: {request.Reason}",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.Transactions.Add(refundTx);

                // Update payment status
                order.PaymentStatus = PaymentStatus.Refunded;
            }
            
            // 6. Release Inventory (Make products available again)
            foreach (var detail in order.OrderDetails)
            {
                detail.Product.Status = ProductStatus.Available;
                detail.Product.UpdatedAt = DateTimeOffset.UtcNow;
            }
            // 7. Save changes and commit transaction
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Order cancelled successfully.";
            
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw; // Ném lỗi ra ngoài cho Controller bắt
        }
        
    }
}