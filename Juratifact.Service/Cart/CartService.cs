using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Cart;

public class CartService: ICartService
{
    
    private readonly AppDbContext _dbContext;
    public CartService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<string> AddProduct(Guid userId, Request.CartRequest request)
    {
        var cart = await _dbContext.Carts
            .Include(c => c.CartDetails)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        var existing = cart?.CartDetails.FirstOrDefault(x => x.ProductId == request.ProductId);

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

    public async Task<string> RemoveProduct(Guid userId, Guid cartDetailId)
    {
        var cartDetail = await _dbContext.CartDetails
            .Include(cd => cd.Cart)
            .FirstOrDefaultAsync(cd => cd.Id == cartDetailId);

        
        if (cartDetail == null)
        {
            throw new ArgumentException("Cart detail not found.");
        }
        
        if (cartDetail.Cart.UserId != userId)
        {
            throw new ArgumentException("You cannot remove the product from the cart");
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