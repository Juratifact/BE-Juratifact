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

    public async Task<string> ApplyProductPromotion(Request.ProductPromotionRequest request)
    {
        // Kiểm tra gói promotion nào còn slot, còn hạn, phù hợp với productId này không
        // Nếu có, tăng UsedSlot lên 1
        var product = _dbContext.Products.Where(x => x.Id == request.ProductId);

        var existingProduct = await product.AnyAsync();

        if (!existingProduct)
        {
            throw new Exception("Product not found");
        }

        var now = DateTimeOffset.UtcNow;

        var promotionPackage = _dbContext.UserPromotionSubscriptions
            .Include(x => x.PromotionPackage)
            .Where(x => x.PromotionPackageId == request.PromotionPackageId
                        && x.PaymentStatus == PaymentStatus.Paid &&
                        x.PromotionPackage.AvailableTo > now &&
                        (x.TotalSlot ?? 0) > (x.UsedSlot ?? 0))
            .OrderBy(x => x.EndTime); // ưu tiên gói nào hết hạn gần nhất

        var subscription = await promotionPackage.FirstOrDefaultAsync();

        if (subscription == null)
        {
            throw new Exception("Promotion package not found");
        }

        // Chặn trùng
        var isDuplicate = await _dbContext.ProductPromotions
            .AnyAsync(p => p.ProductId == request.ProductId &&
                           p.UserPromotionSubscriptionId == subscription.Id &&
                           p.IsActive == true);

        if (isDuplicate)
        {
            throw new Exception("This product is already promoted with this package");
        }

        // dùng được luôn

        if ((subscription.UsedSlot ?? 0) >= (subscription.TotalSlot ?? 0))
        {
            throw new Exception("Promotion package used slot  is too large");
        }

        subscription.UsedSlot = (subscription.UsedSlot ?? 0) + 1;

        var productPromotion = new ProductPromotion()
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            UserPromotionSubscriptionId = subscription.Id,
            IsActive = true,
            ActiveAt = DateTimeOffset.UtcNow,
            ExpiresAt = subscription.PromotionPackage.AvailableTo,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Add(productPromotion);
        await _dbContext.SaveChangesAsync();

        return "Apply product promotion successfully";
    }

    public async Task<string> ChangeStatusPromotion(Guid id)
    {
        var productPromotion = await _dbContext.ProductPromotions
            .Include(x => x.UserPromotionSubscription)
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (productPromotion == null)
        {
            throw new Exception("Product promotion not found");
        }

        if (productPromotion.UserPromotionSubscription.UsedSlot >= productPromotion.UserPromotionSubscription.TotalSlot)
        {
            throw new Exception("Promotion package used slot  is too large");
        }

        var now = DateTimeOffset.UtcNow;

        if (productPromotion.ExpiresAt < now)
        {
            throw new Exception("Promotion package is expired");
        }

        var subscription = productPromotion.UserPromotionSubscription;

        // LOGIC TOGGLE

        if (productPromotion.IsActive)
        {
            // on -> off
            productPromotion.IsActive = false;
            subscription.UsedSlot = (subscription.UsedSlot ?? 0) - 1; //turn back slot
        }
        else
        {
            if ((subscription.UsedSlot ?? 0) >= (subscription.TotalSlot ?? 0))
            {
                throw new Exception("Promotion package used slot  is too large");
            }

            productPromotion.IsActive = true;
            subscription.UsedSlot = (subscription.UsedSlot ?? 0) + 1; // increase slot
        }

        productPromotion.UpdatedAt = DateTimeOffset.UtcNow;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _dbContext.SaveChangesAsync();
        return $"Update promotion status to {(productPromotion.IsActive ? "ON" : "OFF")} successfully";
    }

    public Task<List<Response.GetProductPromotionResponse>> GetProductPromotion()
    {        
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);
        var productPromotions = _dbContext.ProductPromotions
            .Include(p => p.UserPromotionSubscription)
            .Where(p => p.UserPromotionSubscription.UserId == userIdGuid)
            .Select(p => new Response.GetProductPromotionResponse()
            {
                ProductPromotionId = p.Id,
                UserPromotionSubscriptionId = p.UserPromotionSubscriptionId,
                ProductId = p.ProductId,
                IsActive = p.IsActive,
                ActiveAt = p.ActiveAt,
                ExpiresAt = p.ExpiresAt,
            });

        return productPromotions.ToListAsync();
    }
}