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


    public OrderService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor, ISepayService sepayService,
        ISettlementService settlementService, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _httpContext = httpContextAccessor;
        _sepayService = sepayService;
        _settlementService = settlementService;
        _notificationService = notificationService;
    }

    public async Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CheckoutRequest request)
    {
        // Nếu mà gộp lại như thế này thì sellerId nó sẽ ko biết được cái nào là thằng nào hết
        // Sẽ bị lỗi 
        // K thể chạy được


        
        var userIdStr = _httpContext.HttpContext?.User.Claims
            .FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userIdGuid))
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        
        
        
        using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
           
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (user == null) throw new KeyNotFoundException("Account information not found.");
            var identityDocuments = await _dbContext.IdentityDocuments.FirstOrDefaultAsync(x => x.UserId == userIdGuid);
            if (identityDocuments.Status != IdentityStatus.Verified)
            {
                throw new Exception("Your identity document is not verified.");
            }
            
            string? finalAddress = !string.IsNullOrWhiteSpace(request.ShippingAddress)
                ? request.ShippingAddress
                : user.Address;

            if (string.IsNullOrWhiteSpace(finalAddress))
                throw new InvalidOperationException(
                    "Please provide a shipping address. Your profile currently does not have a default address.");

            
            var cart = await _dbContext.Carts
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid && c.IsDeleted == false);

            if (cart == null || cart.CartDetails.All(cd => cd.IsDeleted))
                throw new InvalidOperationException("Your cart is empty, cannot proceed to checkout.");

            var activeItems = cart.CartDetails.Where(cd => cd.IsDeleted == false).ToList();

           
            decimal totalAmount = 0;
            foreach (var item in activeItems)
            {
                if (item.Product.Status != ProductStatus.Available)
                    throw new Exception(
                        $"Product '{item.Product.Title}' is currently not available (Status: {item.Product.Status}).");

                item.Product.Status = ProductStatus.OnHold;
                item.Product.UpdatedAt = DateTimeOffset.UtcNow;
                totalAmount += item.Product.Price;
            }

            if (totalAmount <= 0) throw new Exception("Invalid total order amount.");

        
            var newOrder = new Repository.Entity.Order()
            {
                Id = Guid.NewGuid(),
                UserId = userIdGuid,
                Name = user.FullName,
                ShippingAddress = finalAddress,
                TotalPrice = totalAmount,
                ShippingPee = 0,
                Status = OrderStatus.PendingPayment,
                PaymentStatus = PaymentStatus.UnPaid,
                PaymentMethod = "Banking",
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _dbContext.Orders.Add(newOrder);

          
            foreach (var item in activeItems)
            {
                _dbContext.OrderDetails.Add(new OrderDetail()
                {
                    Id = Guid.NewGuid(),
                    OrderId = newOrder.Id, 
                    ProductId = item.ProductId,
                    Price = item.Product.Price,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

                item.IsDeleted = true;
                item.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // 8. Tạo Transaction
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

           
            await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest()
            {
                UserId = userIdGuid,
                Type = NotificationType.OrderPlaced,
                Data = newOrder.Id.ToString(),
            });

            var qrUrl = await _sepayService.GenerateQrCode(totalAmount, referenceCode);

            return new Response.CreateOrderResponse()
            {
                OrderId = newOrder.Id,
                ReferenceCode = referenceCode,
                QrUrl = qrUrl,
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
        var query = _dbContext.Orders.Where(x => x.Id == id && x.IsDeleted == false);

        var existingOrder = await query.FirstOrDefaultAsync();

        if (existingOrder == null)
        {
            throw new Exception("Order not found");
        }

        var response = new Response.GetOrderStatusResponse()
        {
            Status = existingOrder.Status,
            PaymentStatus = existingOrder.PaymentStatus,
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
                throw new Exception(
                    "Order has already been handed over to the shipping carrier or is completed and cannot be cancelled.");
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

                
                var totalRefund = order.OrderDetails!.Sum(d => d.Price);

             
                wallet.Balance += totalRefund;
                wallet.UpdatedAt = DateTimeOffset.UtcNow;

               
                string refundRefCode =
                    $"RF-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{order.Id.ToString().Substring(0, 8).ToUpper()}";

                
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

               
                order.PaymentStatus = PaymentStatus.Refunded;
            }

           
            foreach (var detail in order.OrderDetails)
            {
                detail.Product.Status = ProductStatus.Available;
                detail.Product.UpdatedAt = DateTimeOffset.UtcNow;
            }

      
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notificationService.SendNotification(new Notification.Request.SendNotificationRequest()
            {
                UserId = userIdGuid,
                Type = NotificationType.OrderCancelled,
                Data = order.Name,
            });

            return "Order cancelled successfully.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw; // Ném lỗi ra ngoài cho Controller bắt
        }
    }

    public async Task<string> CancelCheckout(Guid orderId)
    {
        var userId = _httpContext.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(x => x.Id == orderId
                                          && x.UserId == userIdGuid
                                          && x.IsDeleted == false);

            if (order == null)
            {
                throw new Exception("Order not found or you do not have permission to access it.");
            }

            if (order.Status != OrderStatus.PendingPayment)
            {
                throw new Exception("Only orders waiting for payment can be cancelled via this method.");
            }

           
            order.Status = OrderStatus.Cancelled;
            order.PaymentStatus = PaymentStatus.Failed; // Mark as failed because user aborted checkout
            order.CancelReason = "User cancelled checkout";
            order.UpdatedAt = DateTimeOffset.UtcNow;

        
            var pendingTransactions = await _dbContext.Transactions
                .Where(t => t.OrderId == orderId && t.Status == TransactionStatus.Pending)
                .ToListAsync();

            foreach (var tx in pendingTransactions)
            {
                tx.Status = TransactionStatus.Failed;
                tx.UpdatedAt = DateTimeOffset.UtcNow;
            }

           
            if (order.OrderDetails != null)
            {
                foreach (var detail in order.OrderDetails)
                {
                    detail.Product.Status = ProductStatus.Available;
                    detail.Product.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Checkout cancelled successfully.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Response.GetMyOrderResponse>> GetMyOrder()
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");

       
        var result = await _dbContext.Orders
            .Where(x => x.UserId == userIdGuid && x.IsDeleted == false)
            // Dùng SelectMany để bóc mỗi OrderDetail thành 1 object GetMyOrderResponse riêng biệt
            .SelectMany(order => order.OrderDetails.Select(od => new Response.GetMyOrderResponse()
            {
              
                OrderId = order.Id,
                Name = order.Name,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
            
               
                ProductId = od.ProductId,
                Title = od.Product.Title,
                Condition = od.Product.Condition,
            
                
                Price = od.Price, 
            
               
                SellerId = od.Product.SellerId,
                SellerName = od.Product.Seller.FullName,
                UserName = od.Product.Seller.UserName
            }))
            .ToListAsync(); 

        return result;
    }

    public async Task<Response.ProductListResponse> GetProductbyOrderId(Guid orderId, Guid productId)
    {
        var query = _dbContext.OrderDetails
            .Include(od => od.Product)
            .ThenInclude(p => p.ProductMedias)
            .Where(od => od.OrderId == orderId && od.ProductId == productId);
        
        
        var selected = query.Select(x => new Response.ProductListResponse()
        {
            ProductId = x.Product.Id,
            Title = x.Product.Title,
            Price = x.Price,
            Description = x.Product.Description,
            Condition = x.Product.Condition,
            ImageUrl = x.Product.ProductMedias.Select(m => m.ImageUrl).ToList(),
            Video = x.Product.ProductMedias.Select(m => m.Video!).ToList(),

        });
        var result = await selected.FirstOrDefaultAsync();
        
        if (result == null)
        {
            throw new ArgumentException("Product not found.");
        }
        return result;

    }
}