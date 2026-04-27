using Juratifact.Repository;
using Juratifact.Repository.Enum;
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
    
    public async Task<string> CreatePromotion(Request.PromotionRequest request)
    {
        var existingQuery = _dbContext.PromotionPackages.Where(x => x.PackageName == request.PackageName);
        bool existed = await existingQuery.AnyAsync();

        if (existed)
        {
            throw new ArgumentException("Promotion already exists");
        }

        var promotion = new Repository.Entity.PromotionPackage()
        {
            
            PackageName = request.PackageName,
            Description = request.Description,
            Price = request.Price,
            MaxProductCount = request.MaxProductCount,
            PromotionDaysPerSlot = request.PromotionDaysPerSlot,
            AvailableFrom = request.AvailableFrom,
            AvailableTo = request.AvailableTo,
            UsageLimitDays = request.UsageLimitDays,
        };
        _dbContext.PromotionPackages.Add(promotion);
        await _dbContext.SaveChangesAsync();
        
        return "Promotion created";
    }
    public async Task<Response.PromotionResponse> GetSubscriptions()
    {
        var query = await _dbContext.UserPromotionSubscriptions
            .Where(x => x.PaymentStatus== PaymentStatus.Paid).CountAsync();

        var query2 = await _dbContext.Transactions
            .Where(y => y.TransactionType == TransactionType.ServiceFee).SumAsync(z => z.Amount);

        var result = new Response.PromotionResponse()
        {
            UniqueUsers = query,
            TotalRevenue = query2
        };
        return result;
    }

    public async Task<string> DeletePromotion(Guid id)
    {
        var query = _dbContext.PromotionPackages
            .Where(x => x.Id == id && !x.IsDeleted);
        var result = await query.FirstOrDefaultAsync();

        if (result == null)
        {
            throw new ArgumentException("Promotion not found");
        }
         
        result.IsDeleted = true;
        result.UpdatedAt = DateTimeOffset.Now;
            
        await _dbContext.SaveChangesAsync();
        return  "Promotion deleted";
    }
}