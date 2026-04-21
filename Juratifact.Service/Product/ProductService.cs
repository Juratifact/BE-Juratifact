using Juratifact.Repository;
using Juratifact.Service.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Product;

public class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;
    private readonly IMediaService _mediaService;
    private readonly IHttpContextAccessor _httpContext;


    public ProductService(AppDbContext dbContext, IMediaService mediaService, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _mediaService = mediaService;
        _httpContext = httpContext;
    }
    
    public async Task<string> CreateProduct(ProductRequest.CreateProductRequest request)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);

        // Check if user exists and has Buyer role
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userIdGuid);

        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        // Check if user has Buyer role
        var hasBuyerRole = user.UserRoles.Any(ur => ur.Role.Name == "Buyer");
        if (!hasBuyerRole)
        {
            throw new ArgumentException("User must have Buyer role to create products.");
        }

        // Check if user already has Seller role, if not, add it
        var hasSellerRole = user.UserRoles.Any(ur => ur.Role.Name == "Seller");
        if (!hasSellerRole)
        {
            var sellerRole = await _dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "Seller");

            if (sellerRole == null)
            {
                // Create Seller role if it doesn't exist
                sellerRole = new Repository.Entity.Role()
                {
                    Name = "Seller",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.Roles.Add(sellerRole);
                await _dbContext.SaveChangesAsync();
            }

            // Add Seller role to user
            var userRole = new Repository.Entity.UserRole()
            {
                UserId = user.Id,
                RoleId = sellerRole.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.UserRoles.Add(userRole);
            await _dbContext.SaveChangesAsync();
        }

        // Create product
        var product = new Repository.Entity.Product()
        {
            SellerId = user.Id, // SellerId is the UserId of the product creator
            Title = request.Title,
            Condition = request.Condition,
            Description = request.Description,
            Price = request.Price,
            Status = Enum.TryParse<Juratifact.Repository.Enum.ProductStatus>(request.Status, out var status) ? status : throw new ArgumentException("Invalid product status."),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        // Upload image and create ProductMedia
        string? imageUrl = null;
        string? videoUrl = null;

        if (request.Image != null)
        {
            imageUrl = await _mediaService.UploadAsync(request.Image);
        }

        if (request.Video != null)
        {
            videoUrl = await _mediaService.UploadVideoAsync(request.Video);
        }

        var productMedia = new Repository.Entity.ProductMedia()
        {
            ImageUrl = imageUrl ?? "",
            Video = videoUrl,
            ProductId = product.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ProductMedia.Add(productMedia);
        await _dbContext.SaveChangesAsync();

        return "Product created successfully! User now has both Buyer and Seller roles.";
    }
}