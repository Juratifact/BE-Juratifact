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

    public async Task<Base.Response.PageResult<Response.PromotionPackageResponse>> GetPromotionPackages(int pageSize, int pageIndex)
    {
        var now = DateTimeOffset.UtcNow;

        var query = _dbContext.PromotionPackages
            .Where(pp => pp.AvailableFrom <= now && pp.AvailableTo >= now && pp.IsDeleted == false);
        
        query = query.OrderByDescending(x => x.CreatedAt);
        query = query.Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);
        var selected = query.Select(pp => new Response.PromotionPackageResponse
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
            CreatedAt = pp.CreatedAt,
            UpdatedAt = pp.UpdatedAt
        });
        
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.PromotionPackageResponse>()
        {
            Items = listResult,
            TotalItems = totalItems,
        };
        
        return result;
    }

    public async Task<string> CreatePromotion(Request.PromotionRequest request)
    {
        var existingQuery = _dbContext.PromotionPackages.Where(x => x.PackageName == request.PackageName);
        bool existed = await existingQuery.AnyAsync();

        if (existed)
        {
            throw new ArgumentException("Promotion already exists");
        }

        var promotion = new PromotionPackage()
        {
            PackageName = request.PackageName,
            Description = request.Description,
            Price = request.Price,
            MaxProductCount = request.MaxProductCount,
            PromotionDaysPerSlot = request.PromotionDaysPerSlot,
            AvailableFrom = request.AvailableFrom,
            AvailableTo = request.AvailableTo,
            UsageLimitDays = request.UsageLimitDays,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.PromotionPackages.Add(promotion);
        await _dbContext.SaveChangesAsync();

        return "Promotion created";
    }

    public async Task<string> SoftDeletePromotionPackage(Guid packageId)
    {
        var package = await _dbContext.PromotionPackages
            .FirstOrDefaultAsync(p => p.Id == packageId && p.IsDeleted == false);

        if (package == null)
        {
            throw new KeyNotFoundException("Promotion package not found");
        }

        package.IsDeleted = true;
        package.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.PromotionPackages.Update(package);
        await _dbContext.SaveChangesAsync();

        return "Promotion package deleted";
    }

    public async Task<Response.SubscribeResponse> SubscribeByPackageId(Guid packageId)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);

        var hasAnyProduct = await _dbContext.Products
            .AnyAsync(p => p.SellerId == userIdGuid && p.IsDeleted == false);

        if (!hasAnyProduct)
        {
            throw new InvalidOperationException("Please create a product before subscribing to a promotion package");
        }

        var package = await _dbContext.PromotionPackages.FirstOrDefaultAsync(p => p.Id == packageId  && p.IsDeleted == false);

        if (package == null)
        {
            throw new Exception("Promotion package not found");
        }

        // Check duplicate - tránh tạo 2 lần
        var existingTransaction = await _dbContext.Transactions
            .Where(t => t.TransactionType == TransactionType.ServiceFee &&
                        t.Status == TransactionStatus.Pending)
            .Join(
                _dbContext.UserPromotionSubscriptions,
                t => t.UserPromotionSubscriptionId,
                s => (Guid?)s.Id,
                (t, s) => new { Transaction = t, Subscription = s }
            )
            .Where(x => x.Subscription.UserId == userIdGuid
                        && x.Subscription.PromotionPackageId == packageId
                        && x.Subscription.IsDeleted == false)
            .OrderByDescending(x => x.Transaction.CreatedAt)
            .Select(x => x.Transaction)
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

        
        var now = DateTimeOffset.UtcNow; // Chuẩn hóa thời gian
        // Tạo Subscription trước
        var subscriptionId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        var subscription = new UserPromotionSubscription()
        {
            Id = subscriptionId,
            UserId = userIdGuid,
            PromotionPackageId = packageId,
            PaymentStatus = PaymentStatus.UnPaid,
            TotalSlot = package.MaxProductCount,
            UsedSlot = 0,
            StartTime = now,
            EndTime = now.AddDays(package.UsageLimitDays ?? 30),
            TransactionId = transactionId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Add(subscription);

        // Tạo Transaction sau, gắn SubscriptionId
        var transaction = new Transaction()
        {
            Id = transactionId,
            UserPromotionSubscriptionId = subscriptionId,
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

    public async Task<string> CancelSubscriptionPayment(Guid packageId)
    {
        var userId = _httpContext.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);
        var now = DateTimeOffset.UtcNow;

        var pendingTransactions = await _dbContext.Transactions
            .Include(t => t.UserPromotionSubscription)
            .Where(t => t.TransactionType == TransactionType.ServiceFee &&
                        t.Status == TransactionStatus.Pending &&
                        t.IsDeleted == false &&
                        t.UserPromotionSubscription != null &&
                        t.UserPromotionSubscription.UserId == userIdGuid &&
                        t.UserPromotionSubscription.PromotionPackageId == packageId &&
                        t.UserPromotionSubscription.IsDeleted == false)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        if (pendingTransactions.Count == 0)
        {
            throw new KeyNotFoundException("No pending subscription payment found for this package.");
        }

        foreach (var transaction in pendingTransactions)
        {
            transaction.Status = TransactionStatus.Expired;
            transaction.Description = "Payment canceled by user";
            transaction.UpdatedAt = now;

            if (transaction.UserPromotionSubscription == null)
            {
                continue;
            }

            transaction.UserPromotionSubscription.PaymentStatus = PaymentStatus.Failed;
            transaction.UserPromotionSubscription.IsDeleted = true;
            transaction.UserPromotionSubscription.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        return "Cancel subscription payment successfully";
    }

    public async Task<List<Response.PromotionSubscribeResponse>> GetSubscribedPromotions()
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);

        var promotionPackage = _dbContext.UserPromotionSubscriptions
            .AsNoTracking()
            .Include(s => s.PromotionPackage)
            .Where(p => p.UserId == userIdGuid && p.IsDeleted == false)
            .OrderByDescending(p => p.CreatedAt);

        var selected = promotionPackage.Select(p => new Response.PromotionSubscribeResponse()
        {
            PromotionPackageId = p.PromotionPackageId,
            PromotionPackageName = p.PromotionPackage.PackageName,
            StartTime = p.StartTime,
            EndTime = p.EndTime,
            AvailableFrom = p.PromotionPackage.AvailableFrom,
            AvailableTo = p.PromotionPackage.AvailableTo,
            TotalSlot = p.TotalSlot ?? 0,
            UsedSlot = p.UsedSlot ?? 0,
            Price = p.PromotionPackage.Price,
            PaymentStatus = p.PaymentStatus,
        });

        var list = await selected.ToListAsync();
        return list;
    }

    public async Task<string> ApplyProductPromotion(Request.ProductPromotionRequest request)
    {
        // Kiểm tra gói promotion nào còn slot, còn hạn, phù hợp với productId này không
        // Nếu có, tăng UsedSlot lên 1
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);
        var existingProduct = await _dbContext.Products.AnyAsync(x =>
            x.Id == request.ProductId &&
            x.SellerId == userIdGuid &&
            x.IsDeleted == false);

        if (!existingProduct)
        {
            throw new Exception("Product not found");
        }

        var now = DateTimeOffset.UtcNow;

        var hasActivePromotion = await _dbContext.ProductPromotions
            .AnyAsync(p => p.ProductId == request.ProductId &&
                           p.IsDeleted == false &&
                           p.IsActive &&
                           p.ExpiresAt >= now &&
                           p.UserPromotionSubscription.IsDeleted == false &&
                           p.UserPromotionSubscription.UserId == userIdGuid &&
                           p.UserPromotionSubscription.StartTime <= now &&
                           p.UserPromotionSubscription.EndTime >= now &&
                           (p.UserPromotionSubscription.PaymentStatus == PaymentStatus.Paid ||
                            _dbContext.Transactions.Any(t =>
                                t.TransactionType == TransactionType.ServiceFee &&
                                t.Status == TransactionStatus.Success &&
                                (t.UserPromotionSubscriptionId == p.UserPromotionSubscriptionId ||
                                 (p.UserPromotionSubscription.TransactionId != null &&
                                  t.Id == p.UserPromotionSubscription.TransactionId)))));

        if (hasActivePromotion)
        {
            throw new Exception("This product already has an active promotion");
        }

        var inactivePromotion = await _dbContext.ProductPromotions
            .Include(p => p.UserPromotionSubscription)
            .Where(p => p.ProductId == request.ProductId &&
                        p.IsDeleted == false &&
                        p.IsActive == false &&
                        p.ExpiresAt >= now &&
                        p.UserPromotionSubscription.IsDeleted == false &&
                        p.UserPromotionSubscription.UserId == userIdGuid &&
                        p.UserPromotionSubscription.PromotionPackageId == request.PromotionPackageId &&
                        p.UserPromotionSubscription.StartTime <= now &&
                        p.UserPromotionSubscription.EndTime >= now &&
                        (p.UserPromotionSubscription.PaymentStatus == PaymentStatus.Paid ||
                         _dbContext.Transactions.Any(t =>
                             t.TransactionType == TransactionType.ServiceFee &&
                             t.Status == TransactionStatus.Success &&
                             (t.UserPromotionSubscriptionId == p.UserPromotionSubscriptionId ||
                              (p.UserPromotionSubscription.TransactionId != null &&
                               t.Id == p.UserPromotionSubscription.TransactionId)))))
            .OrderBy(p => p.ExpiresAt)
            .FirstOrDefaultAsync();

        if (inactivePromotion != null)
        {
            var existingSubscription = inactivePromotion.UserPromotionSubscription;

            if ((existingSubscription.UsedSlot ?? 0) >= (existingSubscription.TotalSlot ?? 0))
            {
                throw new Exception("Promotion package used slot  is too large");
            }

            inactivePromotion.IsActive = true;
            inactivePromotion.ActiveAt = now;
            inactivePromotion.UpdatedAt = now;
            existingSubscription.UsedSlot = (existingSubscription.UsedSlot ?? 0) + 1;
            existingSubscription.UpdatedAt = now;

            await _dbContext.SaveChangesAsync();
            return "Apply product promotion successfully";
        }

        var promotionPackage = _dbContext.UserPromotionSubscriptions
            .Include(x => x.PromotionPackage)
            .Where(x => x.UserId == userIdGuid &&
                        x.PromotionPackageId == request.PromotionPackageId &&
                        (x.PaymentStatus == PaymentStatus.Paid ||
                         _dbContext.Transactions.Any(t =>
                             t.TransactionType == TransactionType.ServiceFee &&
                             t.Status == TransactionStatus.Success &&
                             (t.UserPromotionSubscriptionId == x.Id ||
                              (x.TransactionId != null && t.Id == x.TransactionId)))) &&
                        x.StartTime <= now && x.EndTime >= now && // kiểm tra xem promotion còn hạn ko
                        (x.TotalSlot ?? 0) > (x.UsedSlot ?? 0) && x.IsDeleted == false)
            .OrderBy(x => x.EndTime); // ưu tiên gói nào hết hạn gần nhất

        var subscription = await promotionPackage.FirstOrDefaultAsync();

        if (subscription == null)
        {
            throw new Exception("Promotion package not found");
            
        }

        // Chặn trùng
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
            ActiveAt = now,
            ExpiresAt = subscription.EndTime,
            CreatedAt = now,
        };
        _dbContext.Add(productPromotion);
        await _dbContext.SaveChangesAsync();

        return "Apply product promotion successfully";
    }

    public async Task<string> ChangeStatusPromotion(Guid id)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);

        var productPromotion = await _dbContext.ProductPromotions
            .Include(x => x.UserPromotionSubscription)
            .Where(x => x.Id == id && x.IsDeleted == false)
            .FirstOrDefaultAsync();

        if (productPromotion == null)
        {
            throw new Exception("Product promotion not found");
        }

        if (productPromotion.UserPromotionSubscription.UserId != userIdGuid)
        {
            throw new UnauthorizedAccessException("You don't have permission to change this promotion status");
        }

        var now = DateTimeOffset.UtcNow;

        var subscription = productPromotion.UserPromotionSubscription;

        // LOGIC TOGGLE

        if (productPromotion.IsActive)
        {
            // on -> off
            productPromotion.IsActive = false;
            subscription.UsedSlot = Math.Max((subscription.UsedSlot ?? 0) - 1, 0); // turn back slot
        }
        else
        {
            if (productPromotion.ExpiresAt < now)
            {
                throw new Exception("Promotion package is expired");
            }

            var hasOtherActivePromotion = await _dbContext.ProductPromotions
                .AnyAsync(p => p.Id != productPromotion.Id &&
                               p.ProductId == productPromotion.ProductId &&
                               p.IsDeleted == false &&
                               p.IsActive &&
                               p.ExpiresAt >= now &&
                               p.UserPromotionSubscription.IsDeleted == false &&
                               p.UserPromotionSubscription.UserId == userIdGuid &&
                               p.UserPromotionSubscription.StartTime <= now &&
                               p.UserPromotionSubscription.EndTime >= now &&
                               (p.UserPromotionSubscription.PaymentStatus == PaymentStatus.Paid ||
                                _dbContext.Transactions.Any(t =>
                                    t.TransactionType == TransactionType.ServiceFee &&
                                    t.Status == TransactionStatus.Success &&
                                    (t.UserPromotionSubscriptionId == p.UserPromotionSubscriptionId ||
                                     (p.UserPromotionSubscription.TransactionId != null &&
                                      t.Id == p.UserPromotionSubscription.TransactionId)))));

            if (hasOtherActivePromotion)
            {
                throw new Exception("This product already has another active promotion");
            }

            if ((subscription.UsedSlot ?? 0) >= (subscription.TotalSlot ?? 0))
            {
                throw new Exception("Promotion package used slot  is too large");
            }

            productPromotion.IsActive = true;
            productPromotion.ActiveAt = now;
            subscription.UsedSlot = (subscription.UsedSlot ?? 0) + 1; // increase slot
        }

        productPromotion.UpdatedAt = now;
        subscription.UpdatedAt = now;
        
        await _dbContext.SaveChangesAsync();
        return $"Update promotion status to {(productPromotion.IsActive ? "ON" : "OFF")} successfully";
    }

    public Task<List<Response.GetProductPromotionResponse>> GetProductPromotion()
    {        
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        var userIdGuid = Guid.Parse(userId!);
        var productPromotions = _dbContext.ProductPromotions
            .Include(p => p.UserPromotionSubscription)
            .Include(p => p.Product)
            .Where(p => p.UserPromotionSubscription.UserId == userIdGuid &&
                        p.Product.IsDeleted == false &&
                        p.Product.Status != ProductStatus.Sold &&
                        p.IsDeleted == false)
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

    public async Task<List<Response.ProductWithoutPromotionResponse>> GetProductsWithoutPromotion()
    {
        var userId = _httpContext.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);
        var now = DateTimeOffset.UtcNow;

        var activePromotedProductIds = _dbContext.ProductPromotions
            .AsNoTracking()
            .Where(pp => pp.IsDeleted == false &&
                         pp.IsActive &&
                         pp.ExpiresAt >= now &&
                         pp.UserPromotionSubscription.IsDeleted == false &&
                         pp.UserPromotionSubscription.UserId == userIdGuid &&
                         pp.UserPromotionSubscription.StartTime <= now &&
                         pp.UserPromotionSubscription.EndTime >= now &&
                         (pp.UserPromotionSubscription.PaymentStatus == PaymentStatus.Paid ||
                          _dbContext.Transactions.Any(t =>
                              t.TransactionType == TransactionType.ServiceFee &&
                              t.Status == TransactionStatus.Success &&
                              (t.UserPromotionSubscriptionId == pp.UserPromotionSubscriptionId ||
                               (pp.UserPromotionSubscription.TransactionId != null &&
                                t.Id == pp.UserPromotionSubscription.TransactionId)))))
            .Select(pp => pp.ProductId);

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.SellerId == userIdGuid &&
                        p.IsDeleted == false &&
                        p.Status != ProductStatus.Sold &&
                        !activePromotedProductIds.Contains(p.Id))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new Response.ProductWithoutPromotionResponse
            {
                ProductId = p.Id,
                ProductTitle = p.Title,
                ProductPrice = p.Price,
                ProductStatus = p.Status,
                CreatedAt = p.CreatedAt,
                UrlImage = p.ProductMedias
                    .Where(m => !string.IsNullOrEmpty(m.ImageUrl))
                    .Select(m => m.ImageUrl)
                    .ToList()
            })
            .ToListAsync();

        return products;
    }

    public async Task<List<Response.PromotionProductResponse>> GetProductsByPromotionPackageId(Guid promotionPackageId)
    {
        var userId = _httpContext.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == "UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userIdGuid = Guid.Parse(userId);

        var productPromotions = await _dbContext.ProductPromotions
            .AsNoTracking()
            .Where(p => p.UserPromotionSubscription.UserId == userIdGuid &&
                        p.UserPromotionSubscription.PromotionPackageId == promotionPackageId &&
                        p.UserPromotionSubscription.IsDeleted == false &&
                        p.Product.IsDeleted == false &&
                        p.Product.Status != ProductStatus.Sold &&
                        p.IsDeleted == false)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.ActiveAt)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new Response.PromotionProductResponse()
            {
                ProductPromotionId = p.Id,
                PromotionPackageId = promotionPackageId,
                UserPromotionSubscriptionId = p.UserPromotionSubscriptionId,
                ProductId = p.ProductId,
                ProductTitle = p.Product.Title,
                ProductPrice = p.Product.Price,
                ImageUrl = p.Product.ProductMedias
                    .Where(m => !string.IsNullOrEmpty(m.ImageUrl))
                    .Select(m => m.ImageUrl)
                    .ToList(),
                IsActive = p.IsActive,
                ActiveAt = p.ActiveAt,
                ExpiresAt = p.ExpiresAt,
            })
            .ToListAsync();

        var seenProductIds = new HashSet<Guid>();
        var unique = new List<Response.PromotionProductResponse>(productPromotions.Count);

        foreach (var item in productPromotions)
        {
            if (seenProductIds.Add(item.ProductId))
            {
                unique.Add(item);
            }
        }

        return unique;
    }
}
