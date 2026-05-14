using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.Notification;
using Juratifact.Service.Sepay;
using Juratifact.Service.SettlementService;
using Juratifact.Service.VietMap;
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
    private readonly IVietMapService _vietMapService;


    public OrderService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor, ISepayService sepayService,
        ISettlementService settlementService, INotificationService notificationService, IVietMapService vietMapService)
    {
        _dbContext = dbContext;
        _httpContext = httpContextAccessor;
        _sepayService = sepayService;
        _settlementService = settlementService;
        _notificationService = notificationService;
        _vietMapService = vietMapService;
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
            if (identityDocuments == null || identityDocuments.Status != IdentityStatus.Verified)
            {
                throw new Exception("Your identity document is not verified.");
            }
            
            string? finalAddress;
            string? vietMapRefId = null;
            double? shippingLatitude = null;
            double? shippingLongitude = null;

            if (!string.IsNullOrWhiteSpace(request.VietMapRefId))
            {
                var place = await _vietMapService.GetPlaceDetailAsync(request.VietMapRefId);
                finalAddress = place.Display;
                vietMapRefId = request.VietMapRefId;
                shippingLatitude = place.Latitude;
                shippingLongitude = place.Longitude;
            }
            else
            {
                finalAddress = !string.IsNullOrWhiteSpace(request.ShippingAddress)
                    ? request.ShippingAddress
                    : user.Address;
            }

            if (string.IsNullOrWhiteSpace(finalAddress))
                throw new InvalidOperationException(
                    "Please provide a shipping address. Your profile currently does not have a default address.");

            
            var cart = await _dbContext.Carts
                .Include(c => c.CartDetails)
                .ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userIdGuid && c.IsDeleted == false);

            if (cart == null || cart.CartDetails.All(cd => cd.IsDeleted))
                throw new InvalidOperationException("Your cart is empty, cannot proceed to checkout.");

            var selectedCartDetailIds = request.CartDetailIds?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            var activeItems = cart.CartDetails
                .Where(cd => cd.IsDeleted == false)
                .ToList();

            if (selectedCartDetailIds is { Count: > 0 })
            {
                activeItems = activeItems
                    .Where(cd => selectedCartDetailIds.Contains(cd.Id))
                    .ToList();

                if (activeItems.Count != selectedCartDetailIds.Count)
                    throw new InvalidOperationException("One or more selected cart items were not found in your cart.");
            }

            if (activeItems.Count == 0)
                throw new InvalidOperationException("Please select at least one cart item to checkout.");

           
            decimal subtotalAmount = 0;
            foreach (var item in activeItems)
            {
                if (item.Product.Status != ProductStatus.Available)
                    throw new Exception(
                        $"Product '{item.Product.Title}' is currently not available (Status: {item.Product.Status}).");

                item.Product.Status = ProductStatus.OnHold;
                item.Product.UpdatedAt = DateTimeOffset.UtcNow;
                subtotalAmount += item.Product.Price * item.Quantity;
            }

            if (subtotalAmount <= 0) throw new Exception("Invalid total order amount.");

            var shippingFee = 0m;
            var discountAmount = 0m;
            var totalAmount = subtotalAmount + shippingFee - discountAmount;
            var orderCode = $"ORD-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            const decimal platformFeeRate = 0.05m;

        
            var newOrder = new Repository.Entity.Order()
            {
                Id = Guid.NewGuid(),
                UserId = userIdGuid,
                Name = orderCode,
                ShippingAddress = finalAddress,
                VietMapRefId = vietMapRefId,
                ShippingLatitude = shippingLatitude,
                ShippingLongitude = shippingLongitude,
                SubtotalPrice = subtotalAmount,
                TotalPrice = totalAmount,
                ShippingFee = shippingFee,
                DiscountAmount = discountAmount,
                Status = OrderStatus.PendingPayment,
                PaymentStatus = PaymentStatus.UnPaid,
                PaymentMethod = "Banking",
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _dbContext.Orders.Add(newOrder);

            var sellerOrderIds = new List<Guid>();
            var sellerGroups = activeItems
                .GroupBy(item => item.Product.SellerId)
                .ToList();

            var sellerOrderIndex = 1;
            foreach (var sellerGroup in sellerGroups)
            {
                var sellerSubtotal = sellerGroup.Sum(item => item.Product.Price * item.Quantity);
                var sellerShippingFee = 0m;
                var sellerDiscountAmount = 0m;
                var platformFee = Math.Round(sellerSubtotal * platformFeeRate, 2);
                var sellerOrder = new SellerOrder
                {
                    Id = Guid.NewGuid(),
                    Code = $"{orderCode}-S{sellerOrderIndex:D2}",
                    OrderId = newOrder.Id,
                    SellerId = sellerGroup.Key,
                    SubtotalPrice = sellerSubtotal,
                    ShippingFee = sellerShippingFee,
                    DiscountAmount = sellerDiscountAmount,
                    TotalPrice = sellerSubtotal + sellerShippingFee - sellerDiscountAmount,
                    PlatformFee = platformFee,
                    SellerReceivableAmount = sellerSubtotal - platformFee,
                    Status = OrderStatus.PendingPayment,
                    CreatedAt = DateTimeOffset.UtcNow,
                };

                _dbContext.SellerOrders.Add(sellerOrder);
                sellerOrderIds.Add(sellerOrder.Id);
                sellerOrderIndex++;

                foreach (var item in sellerGroup)
                {
                    _dbContext.OrderDetails.Add(new OrderDetail()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = newOrder.Id,
                        SellerOrderId = sellerOrder.Id,
                        ProductId = item.ProductId,
                        Price = item.Product.Price,
                        Quantity = item.Quantity,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });

                    item.IsDeleted = true;
                    item.UpdatedAt = DateTimeOffset.UtcNow;
                }
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
                SellerOrderIds = sellerOrderIds,
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

        query = query.OrderByDescending(x => x.CreatedAt);
        var existingOrder = await query.FirstOrDefaultAsync();

        if (existingOrder == null)
        {
            throw new Exception("Order not found");
        }

        var response = new Response.GetOrderStatusResponse()
        {
            Status = existingOrder.Status,
            PaymentStatus = existingOrder.PaymentStatus,
            CreatedAt = existingOrder.CreatedAt,
            UpdatedAt = existingOrder.UpdatedAt
        };

        return response;
    }

    public async Task<List<Response.GetAllOrderResponse>> GetAllOrders()
    {
        var query = _dbContext.Orders.Where(x => x.IsDeleted == false);
        query = query.OrderByDescending(x => x.CreatedAt);
        var select = query.Select(x => new Response.GetAllOrderResponse()
        {
            OrderId = x.Id,
            Name = x.Name,
            Status = x.Status,
            PaymentStatus = x.PaymentStatus,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        });

        var result = await select.ToListAsync();
        return result;
    }

    public async Task<string> ConfirmReceipt(Guid orderId)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);

        var order = await _dbContext.Orders
            .Include(o => o.SellerOrders)
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
                .Include(o => o.SellerOrders)
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

            foreach (var sellerOrder in order.SellerOrders)
            {
                sellerOrder.Status = OrderStatus.Cancelled;
                sellerOrder.CancelReason = request.Reason;
                sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // 5. Process Refund (ONLY IF ALREADY PAID)
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                // Find buyer's wallet
                var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userIdGuid);
                if (wallet == null)
                    throw new Exception("Buyer's wallet not found for refund.");

                
                var totalRefund = order.OrderDetails!.Sum(d => d.Price * d.Quantity);

             
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
                .Include(o => o.SellerOrders)
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

            foreach (var sellerOrder in order.SellerOrders)
            {
                sellerOrder.Status = OrderStatus.Cancelled;
                sellerOrder.CancelReason = "User cancelled checkout";
                sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
            }

        
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
            .OrderByDescending(x => x.CreatedAt)
            // Dùng SelectMany để bóc mỗi OrderDetail thành 1 object GetMyOrderResponse riêng biệt
            .SelectMany(order => order.OrderDetails.Select(od => new Response.GetMyOrderResponse()
            {
              
                OrderId = order.Id,
                Name = order.Name,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
            
               
                ProductId = od.ProductId,
                Title = od.Product.Title,
                Condition = od.Product.Condition,
            
                
                Price = od.Price * od.Quantity,
                SellerOrderId = od.SellerOrderId,
            
               
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

    public async Task<string> UpdateShippingAddress(Guid orderId, Request.UpdateShippingAddressRequest request)
    {
        var userId = _httpContext.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");

        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(x => x.Id == orderId
                                      && x.UserId == userIdGuid
                                      && x.IsDeleted == false);

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found or you do not have permission to access it.");
        }

        
        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            throw new InvalidOperationException("Shipping address can only be updated for paid orders.");
        }

        if ((int)order.Status >= (int)OrderStatus.Shipping)
        {
            throw new InvalidOperationException("Cannot update shipping address once the order has been handed over to the shipper.");
        }

        
        if (!string.IsNullOrWhiteSpace(request.VietMapRefId))
        {
            var place = await _vietMapService.GetPlaceDetailAsync(request.VietMapRefId);
            if (place == null)
                throw new InvalidOperationException("VietMap reference is invalid or could not be resolved.");

            order.ShippingAddress = place.Display;
            order.VietMapRefId = request.VietMapRefId;
            order.ShippingLatitude = place.Latitude;
            order.ShippingLongitude = place.Longitude;
        }
        else
        {
            
            order.ShippingAddress = request.NewAddress;
            order.VietMapRefId = null;
            order.ShippingLatitude = null;
            order.ShippingLongitude = null;
        }

        order.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Orders.Update(order);
        await _dbContext.SaveChangesAsync();

        return "Shipping address updated successfully.";
    }
}
