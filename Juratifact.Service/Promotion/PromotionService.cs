using Juratifact.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Promotion;

public class PromotionService : IPromotionService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public PromotionService(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }

    public async Task<List<Response.PromotionPackageResponse>> GetPromotionPackages()
    {
        var now = DateTime.UtcNow;

        var promotionPackages = await _dbContext.PromotionPackages
            .Where(pp => pp.AvailableFrom <= now && pp.AvailableTo >= now)
            .Select(pp => new Response.PromotionPackageResponse
            {
                PackageId = pp.Id,
                PackageName = pp.PackageName,
                Price = pp.Price,
                MaxProductCount = pp.MaxProductCount,
                PromotionDaysPerSlot = pp.PromotionDaysPerSlot,
                UsageLimitDays = pp.UsageLimitDays, //Tùy business
                Description = pp.Description,
                AvailableFrom = pp.AvailableFrom,
                AvailableTo = pp.AvailableTo,
            })
            .ToListAsync();
        
        return promotionPackages;
    }
}