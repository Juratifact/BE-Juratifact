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

    public async Task<Base.Response.PageResult<Response.GetCartResponse>> GetMyCart(int pageIndex, int pageSize)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        }

        var query = _dbContext.CartDetails
            .Include(cd => cd.Product)
            .ThenInclude(p => p.ProductMedias)
            .Include(cd => cd.Product)
            .ThenInclude(p => p.Seller)
            .Where(cd => cd.Cart.UserId == userIdGuid && cd.IsDeleted == false && cd.Cart.IsDeleted == false);
        
        query = query.OrderByDescending(cd => cd.CreatedAt);
        query = query.Where(x => x.Cart.IsDeleted == false);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
        var totalItems = await query.CountAsync();



        var items = await query.Select(cd => new Response.GetCartResponse
        {
            CartDetailId = cd.Id,
            ProductId = cd.ProductId,
            ProductTitle = cd.Product.Title,
            ProductImageUrls = cd.Product.ProductMedias
                .Where(pm => pm.IsDeleted == false)
                .Select(pm => pm.ImageUrl).ToList(),
            ProductVideoUrls = cd.Product.ProductMedias
                .Where(pm => pm.IsDeleted == false)
                .Select(pm => pm.Video).ToList(),
            Price = cd.Product.Price,
            Quantity = cd.Quantity,
            Condition = cd.Product.Condition,
            SellerId = cd.Product.SellerId,
            SellerName = cd.Product.Seller.FullName,
            AddedAt = cd.CreatedAt
        }).ToListAsync();
            
        

        return new Base.Response.PageResult<Response.GetCartResponse>
        {
            Items = items,
            TotalItems = totalItems,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
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