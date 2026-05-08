using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Cart;

public class CartService : ICartService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public CartService(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<List<Response.GetCartResponse>> GetMyCart()
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }


        // 2. Lấy dữ liệu trực tiếp và Map thẳng sang Response
        var cartItems = await _dbContext.Carts
            .Where(c => c.UserId == userIdGuid && c.IsDeleted == false)
            .SelectMany(c => c.CartDetails) // Làm phẳng: Lấy toàn bộ CartDetails của cái Cart này
            .Where(cd => cd.IsDeleted == false) // Đảm bảo sản phẩm trong giỏ chưa bị xóa
            .Select(cd => new Response.GetCartResponse()
            {
                ProductId = cd.ProductId,
                Title = cd.Product.Title,
                Condition = cd.Product.Condition,
                Price = cd.Product.Price,
                SellerId = cd.Product.SellerId,
                UserName = cd.Product.Seller.UserName,
                SellerName = cd.Product.Seller.FullName
            })
            .ToListAsync();

        return cartItems;
     }
     
     public async Task<string> AddProduct(Guid userId, Request.CartRequest request)
    {
        var cart = await _dbContext.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId);

        var existing = await _dbContext.CartDetails
            .FirstOrDefaultAsync(x => x.CartId == cart.Id 
                                      && x.ProductId == request.ProductId && x.IsDeleted == false);

        if (existing != null)
        {
            existing.Quantity += request.Quantity;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            
            await _dbContext.SaveChangesAsync();
            return "Updated Quantity Successfully.";
        }

        var newProduct = new CartDetail()
        {
            Id = Guid.NewGuid(),
            CartId = cart!.Id,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.CartDetails.Add(newProduct);
        await _dbContext.SaveChangesAsync();
    
        return "New product added to cart successfully.";
    }

    public async Task<string> RemoveProduct(Guid userId, Guid productId)
    {
        var cartDetail = await _dbContext.CartDetails
            .FirstOrDefaultAsync(cd => cd.ProductId == productId && cd.Cart.UserId == userId);
        
        if (cartDetail == null)
        {
            throw new ArgumentException("Cart detail not found.");
        }
        
        _dbContext.CartDetails.Remove(cartDetail);
    
        var result = await _dbContext.SaveChangesAsync();

        if (result > 0)
        {
            return "Remove successfully.";
        }
        return "Remove failed.";
    }
     
    
}