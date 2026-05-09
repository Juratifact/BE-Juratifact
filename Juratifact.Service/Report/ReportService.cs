using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Juratifact.Service.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Report;

public class ReportService: IReportService
{
    private readonly AppDbContext _dbContext;
    
    private readonly IHttpContextAccessor _httpContext;

    public ReportService(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<string> CreateReport(Request.ReportRequest request)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);

        // Check if user has Buyer role
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userIdGuid);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        var hasBuyerRole = user.UserRoles.Any(ur => ur.Role.Name == "Buyer");
                                                     
        if (!hasBuyerRole)
        {
            throw new ArgumentException("User must have a Buyer Role.");
        }
        
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId);

        if (product == null)
        {
            throw new ArgumentException("Product not found.");
        }

        if (product.SellerId == userIdGuid)
        {
            throw new ArgumentException("You cannot report this product.");
        }

        var report = new Repository.Entity.Report()
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            UserId = user.Id,
            Reason = request.Reason,
            Description = request.Description,
            Status = ReportStatus.Processing

        };
        _dbContext.Add(report);
        await _dbContext.SaveChangesAsync();
            
        return "Report created successfully";
    }

    public async Task<Base.Response.PageResult<Response.ReportResponse>> GetReport(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.Reports
            .Include(x => x.User)
            .Include(x => x.Product)
                .ThenInclude(p => p.Seller)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductMedias)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductPromotions)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductComments)
            .Where(x => x.Product.Status == ProductStatus.Available);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(x => x.Reason.Contains(searchTerm));
        }

        var totalItems = await query.CountAsync();

        var listResult = await query
            .OrderBy(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.ReportResponse()
            {
                Id = x.Id,
                Reporter = new Response.UserResponse()
                {
                    Id = x.User.Id,
                    FullName = x.User.FullName,
                    Email = x.User.Email,
                    PhoneNumber = x.User.PhoneNumber,
                    ProfilePicture = x.User.ProfilePicture
                },
                Reason = x.Reason,
                Description = x.Description,
                Status = x.Status,
                Product = new Response.ProductDetailResponse()
                {
                    Id = x.Product.Id,
                    Seller = x.Product.Seller != null ? new Response.UserResponse()
                    {
                        Id = x.Product.Seller.Id,
                        FullName = x.Product.Seller.FullName,
                        Email = x.Product.Seller.Email,
                        PhoneNumber = x.Product.Seller.PhoneNumber,
                        ProfilePicture = x.Product.Seller.ProfilePicture
                    } : null,
                    Title = x.Product.Title,
                    Condition = x.Product.Condition,
                    Description = x.Product.Description,
                    Price = x.Product.Price,
                    Status = x.Product.Status,
                    CreatedAt = x.Product.CreatedAt,
                    UpdatedAt = x.Product.UpdatedAt,
                    ImageUrl = x.Product.ProductMedias.Select(m => m.ImageUrl).ToList(),
                    Video = x.Product.ProductMedias.Select(m => m.Video).ToList(),
                    ProductCategories = x.Product.ProductCategories.Select(pc => new Response.CategoryResponse()
                    {
                        CategoryId = pc.CategoryId,
                        CategoryName = pc.Category.Name
                    }).ToList()
                }
            })
            .ToListAsync();

        var result = new Base.Response.PageResult<Response.ReportResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
    }
    

    public async Task<string> ApproveReport(Guid id)
    {
        
        var report = await _dbContext.Reports.FirstOrDefaultAsync(x => x.Id == id);
        if (report == null)
        {
            throw new ArgumentException("Report not found.");
        }
        
        report.Status = ReportStatus.Approved;
        report.UpdatedAt = DateTime.UtcNow;
        _dbContext.Update(report);
        await _dbContext.SaveChangesAsync();
        
        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == report.ProductId);
        if (product == null)
        {
            throw new ArgumentException("Product not found.");
        }
        
        product.Status = ProductStatus.Banned;
        product.UpdatedAt = DateTime.UtcNow;
        _dbContext.Update(product);
        await _dbContext.SaveChangesAsync();
        return "Report approved successfully";
        
    }

    public async Task<string> RejectReport(Guid id)
    {
        var report = await _dbContext.Reports.FirstOrDefaultAsync(x => x.Id == id);
        if (report == null)
        {
            throw new ArgumentException("Report not found.");
        }
        
        report.Status = ReportStatus.Rejected;
        report.UpdatedAt = DateTime.UtcNow;
        _dbContext.Update(report);
        var result = await _dbContext.SaveChangesAsync();
        if (result > 0)
        {
            return "Report rejected successfully";
        }
        return "Report rejected not successfully";

    }

    public async Task<Response.ReportResponse> GetProductByReportId(Guid reportId)
    {
        var report = _dbContext.Reports
            .Include(x => x.User)
            .Include(x => x.Product)
                .ThenInclude(p => p.Seller)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductMedias)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductPromotions)
            .Include(x => x.Product)
                .ThenInclude(p => p.ProductComments)
            .Where(x => x.Id == reportId);

        var result = await report.Select(x => new Response.ReportResponse()
        {
            Id = x.Id,
            Reporter = new Response.UserResponse()
            {
                Id = x.User.Id,
                FullName = x.User.FullName,
                Email = x.User.Email,
                PhoneNumber = x.User.PhoneNumber,
                ProfilePicture = x.User.ProfilePicture
            },
            Reason = x.Reason,
            Description = x.Description,
            Status = x.Status,
            Product = new Response.ProductDetailResponse()
            {
                Id = x.Product.Id,
                Seller = x.Product.Seller! != null ? new Response.UserResponse()
                {
                    Id = x.Product.Seller.Id,
                    FullName = x.Product.Seller.FullName,
                    Email = x.Product.Seller.Email,
                    PhoneNumber = x.Product.Seller.PhoneNumber,
                    ProfilePicture = x.Product.Seller.ProfilePicture
                } : null,
                Title = x.Product.Title,
                Condition = x.Product.Condition,
                Description = x.Product.Description,
                Price = x.Product.Price,
                Status = x.Product.Status,
                CreatedAt = x.Product.CreatedAt,
                UpdatedAt = x.Product.UpdatedAt,
                ImageUrl = x.Product.ProductMedias.Select(m => m.ImageUrl).ToList(),
                Video = x.Product.ProductMedias.Select(m => m.Video).ToList(),
                ProductCategories = x.Product.ProductCategories.Select(pc => new Response.CategoryResponse()
                {
                    CategoryId = pc.CategoryId,
                    CategoryName = pc.Category.Name
                }).ToList()
            }
        }).FirstOrDefaultAsync();

        if (result == null)
        {
            throw new ArgumentException("Report not found.");
        }

        return result;
    }
}