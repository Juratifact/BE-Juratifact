using Juratifact.Repository;
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
            Status = request.Status,

        };
        _dbContext.Add(report);
        await _dbContext.SaveChangesAsync();
            
        return "Report created successfully";
    }

    public async Task<Base.Response.PageResult<Response.ReportResponse>> GetReport(string? searchTerm,int pageSize, int pageIndex)
    {
        var query = _dbContext.Reports.Where(x => true);

        
        if (searchTerm != null)
        {
            query = query.Where(x => x.Reason.Contains(searchTerm));
        }
        query = query.OrderBy(x => x.CreatedAt);
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selectedReport = query.Select(x => new Response.ReportResponse()
        {
            Id = x.Id,
            ProductId = x.ProductId,
            UserId = x.UserId,
            Reason = x.Reason,
            Description = x.Description,
            Status = x.Status,

        });
        
        var listResult = await selectedReport.ToListAsync();
        var totalItems = listResult.Count;
 
        var result = new Base.Response.PageResult<Response.ReportResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;

    }
}