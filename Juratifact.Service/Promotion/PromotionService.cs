using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.Sepay;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Promotion;

public class PromotionService : IPromotionService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ISepayService _sepayService;

    public PromotionService(AppDbContext dbContext, IHttpContextAccessor httpContext, ISepayService sepayService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _sepayService = sepayService;
    }

    public async Task<List<Response.PromotionPackageResponse>> GetPromotionPackages()
    {
        var now = DateTimeOffset.UtcNow;

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

    public async Task<Response.SubscribeResponse> SubscribeByPackageId(Guid packageId)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);

        var package = await _dbContext.PromotionPackages.FirstOrDefaultAsync(p => p.Id == packageId);

        if (package == null)
        {
            throw new Exception("Promotion package not found");
        }

        // Check duplicate - tránh tạo 2 lần
        var existingTransaction = await _dbContext.Transactions
            .Where(t => t.UserPromotionSubscription!.UserId == userIdGuid
                        && t.UserPromotionSubscription.PromotionPackageId == packageId
                        && t.Status == TransactionStatus.Pending)
            .FirstOrDefaultAsync();

        if (existingTransaction != null)
        {
            var existingQr = await _sepayService.GenerateQrCode(
                existingTransaction.Amount,
                existingTransaction.ReferenceCode);

            return new Response.SubscribeResponse()
            {
                QrUrl = existingQr
            };
        }

        // ReferenceCode unique
        var referenceCode = $"JURATIFACT{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Tạo Subscription trước
        var subscription = new UserPromotionSubscription()
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            PromotionPackageId = packageId,
            PaymentStatus = PaymentStatus.UnPaid,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Add(subscription);
        await _dbContext.SaveChangesAsync();
        
        // Tạo Transaction sau, gắn SubscriptionId
        var transaction = new Transaction()
        {
            Id = Guid.NewGuid(),
            UserPromotionSubscriptionId = subscription.Id,
            UserPromotionSubscription = subscription,
            TransactionType = TransactionType.ServiceFee,
            Status = TransactionStatus.Pending,
            ReferenceCode = referenceCode,
            Amount = package.Price,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.Add(transaction);
        await _dbContext.SaveChangesAsync();
        
        var qrUrl = await _sepayService.GenerateQrCode(package.Price, referenceCode);
        
        var result = new Response.SubscribeResponse()
        {
            QrUrl = qrUrl
        };

        return result;
    }

    public async Task<List<Response.PromotionSubscribeResponse>> GetSubscribedPromotions()
    {
        var now = DateTimeOffset.UtcNow;

        var promotionPackage = _dbContext.UserPromotionSubscriptions
            .Include(s => s.PromotionPackage)
            .Where(p => p.PaymentStatus == PaymentStatus.Paid &&
                        p.PromotionPackage.AvailableTo > now &&
                        (p.TotalSlot ?? 0) > (p.UsedSlot ?? 0));

        var selected = promotionPackage.Select(p => new Response.PromotionSubscribeResponse()
        {
            PromotionPackageId = p.PromotionPackageId,
            PromotionPackageName = p.PromotionPackage.PackageName,
            AvailableFrom = p.PromotionPackage.AvailableFrom,
            AvailableTo = p.PromotionPackage.AvailableTo,
            TotalSlot = p.TotalSlot ?? 0,
            UsedSlot = p.UsedSlot ?? 0,
            Price = p.PromotionPackage.Price,
        });
        
        var list = await selected.ToListAsync();
        return list;
    }
}