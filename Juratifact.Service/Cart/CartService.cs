using Juratifact.Repository;
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
}