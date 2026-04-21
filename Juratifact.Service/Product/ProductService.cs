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
            Status = Enum.TryParse<Juratifact.Repository.Enum.ProductStatus>(request.Status, out var status)
                ? status
                : throw new ArgumentException("Invalid product status."),
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

    public async Task<ProductResponse.ProductCommentResponse> CreateComment(ProductRequest.ProductCommentRequest request)
    {
        // Get authenticated user
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);

        // Check if product exists
        var product = await _dbContext.Products.FindAsync(request.ProductId);

        if (product == null)
        {
            throw new ArgumentException("Product not found.");
        }

        // Check if user exists
        var user = await _dbContext.Users.FindAsync(userIdGuid);

        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        // If ParentCommentId is provided, check if parent comment exists and belongs to the same product
        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _dbContext.ProductComments.FindAsync(request.ParentCommentId.Value);

            if (parentComment == null)
            {
                throw new ArgumentException("Parent comment not found.");
            }

            if (parentComment.ProductId != request.ProductId)
            {
                throw new ArgumentException("Parent comment does not belong to this product.");
            }
        }

        var newComment = new Repository.Entity.ProductComment()
        {
            ProductId = request.ProductId,
            UserId = userIdGuid,
            Content = request.Content,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Add(newComment);
        await _dbContext.SaveChangesAsync();

        var commentResponse = new ProductResponse.ProductCommentResponse()
        {
            CommentId = newComment.Id,
            ProductId = newComment.ProductId,
            ProductName = product.Title,
            Content = newComment.Content,
            UserName = user.FullName,
            ParentCommentId = newComment.ParentCommentId
        };

        return commentResponse;
    }

    public async Task<string> UpdateProductPostingById(Guid productId, ProductRequest.UpdateProductRequest request)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);

        // Check if user has Seller role
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userIdGuid);

        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        var hasSellerRole = user.UserRoles.Any(ur => ur.Role.Name == "Seller");
        if (!hasSellerRole)
        {
            throw new ArgumentException("User must have Seller role to update products.");
        }

        // Get existing product - must belong to the authenticated user
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == productId && x.SellerId == userIdGuid);

        if (product == null)
        {
            throw new ArgumentException("Product not found or you don't have permission to update it.");
        }

        // Update product fields
        product.Title = request.Title;
        product.Condition = request.Condition;
        product.Description = request.Description;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync();

        // Update ProductMedia
        var existingProductMedia = await _dbContext.ProductMedia
            .FirstOrDefaultAsync(pm => pm.ProductId == productId);

        if (request.Image != null || request.Video != null)
        {
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

            if (existingProductMedia != null)
            {
                // Update existing ProductMedia
                if (imageUrl != null)
                    existingProductMedia.ImageUrl = imageUrl;

                if (videoUrl != null)
                    existingProductMedia.Video = videoUrl;

                existingProductMedia.UpdatedAt = DateTimeOffset.UtcNow;
                _dbContext.ProductMedia.Update(existingProductMedia);
            }
            else
            {
                // Create new ProductMedia if it doesn't exist
                var newProductMedia = new Repository.Entity.ProductMedia()
                {
                    ImageUrl = imageUrl ?? "",
                    Video = videoUrl,
                    ProductId = productId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.ProductMedia.Add(newProductMedia);
            }

            await _dbContext.SaveChangesAsync();
        }

        return "Product updated successfully!";
    }
    
}