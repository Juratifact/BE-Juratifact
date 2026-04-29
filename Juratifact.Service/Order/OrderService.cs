using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.Sepay;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Order;

public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ISepayService _sepayService;
    
    public OrderService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor, ISepayService sepayService)
    {
        _dbContext = dbContext;
        _httpContext = httpContextAccessor;
        _sepayService = sepayService;
    }
    
    public async Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CreateOrderRequest request)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        
        var userIdGuid = Guid.Parse(userId!);
        
        // 1. Get or create Cart
        var cart = await _dbContext.Carts.FirstOrDefaultAsync(x => x.UserId == userIdGuid);

        if (cart == null)
        {
            cart = new Repository.Entity.Cart()
            {
                Id = Guid.NewGuid(),
                UserId = userIdGuid,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _dbContext.Carts.Add(cart);
        }
        
        //  2. Validate all requested products exist
        var productGroups = request.Products
            .GroupBy(x => x.ProductId)
            .ToList();
        var productIds = productGroups.Select(g => g.Key).ToList();
        
        var products = await _dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            throw new Exception("Some products not found");
        }
        
        // 3. Create CartDetails (one per product)
        foreach (var group in productGroups)
        {
            var quantity = group.Count();
            var newCartDetail = new CartDetail()
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = group.Key,
                Quantity = quantity,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _dbContext.Add(newCartDetail);
            await _dbContext.SaveChangesAsync();
        }
        
        // 4. TotalAmount + OrderDetails
        decimal totalAmount = 0;
        
        foreach (var group in productGroups)
        {
            var quantity = group.Count();
            var product = products.FirstOrDefault(p => p.Id == group.Key)!;
            totalAmount += product.Price * quantity;
        }
        
        if (totalAmount <= 0)
            throw new Exception("Total amount must be greater than 0");
        // 5. Create Order
        
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
        await _dbContext.SaveChangesAsync();

        foreach (var group in productGroups)
        {
            var quantity = group.Count();
            var product = products.FirstOrDefault(p => p.Id == group.Key)!;
    
            totalAmount += product.Price * quantity;
            
            

            var newOrderDetail = new OrderDetail()
            {
                Id = Guid.NewGuid(),
                OrderId = newOrder.Id,
                ProductId = group.Key,
                Price = product.Price * quantity,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            
            _dbContext.Add(newOrderDetail);
            await _dbContext.SaveChangesAsync();
        }
        
        // 7. Generate reference code and create Transaction
        
        var referenceCode = $"JURATIFACT{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        
        var newTransaction = new Transaction()
        {
            Id = Guid.NewGuid(),
            OrderId = newOrder.Id,
            ReferenceCode = referenceCode,          
            Amount = totalAmount,
            Status = TransactionStatus.Pending,
            TransactionType = TransactionType.OrderPayment,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        
        _dbContext.Transactions.Add(newTransaction);
        await _dbContext.SaveChangesAsync();
        
        var qrUrl = await _sepayService.GenerateQrCode(totalAmount, referenceCode);

        return new Response.CreateOrderResponse()
        {
            OrderId  = newOrder.Id,
            QrUrl = qrUrl,
            ReferenceCode = referenceCode,
        };

    }
}