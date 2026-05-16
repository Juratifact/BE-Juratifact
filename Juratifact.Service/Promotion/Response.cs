using Juratifact.Repository.Enum;

namespace Juratifact.Service.Promotion;

public class Response
{
    public class PromotionPackageResponse
    {
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public decimal Price { get; set; }
        public int? MaxProductCount { get; set; }
        public int? PromotionDaysPerSlot { get; set; }
        public int? UsageLimitDays { get; set; } // thời gian dùng trong 1 ngày
        public string? Description { get; set; }
        public DateTimeOffset? AvailableFrom { get; set; }
        public DateTimeOffset? AvailableTo { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }

    public class SubscribeResponse
    {
        public required string QrUrl { get; set; }
    }

    public class PromotionSubscribeResponse
    {
        public Guid PromotionPackageId { get; set; }
        public string PromotionPackageName { get; set; } = null!;
        public decimal Price { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public int TotalSlot { get; set; }
        public int UsedSlot { get; set; }

        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }

        public DateTimeOffset? AvailableFrom { get; set; }
        public DateTimeOffset? AvailableTo { get; set; }
    }

    public class GetProductPromotionResponse
    {
        public Guid ProductPromotionId { get; set; }
        public Guid UserPromotionSubscriptionId { get; set; }
        public Guid ProductId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? ActiveAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public class PromotionProductResponse
    {
        public Guid ProductPromotionId { get; set; }
        public Guid PromotionPackageId { get; set; }
        public Guid UserPromotionSubscriptionId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = null!;
        public decimal ProductPrice { get; set; }
        public bool IsActive { get; set; }
        public List<string> ImageUrl { get; set; } = new();
        public DateTimeOffset? ActiveAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public class ProductWithoutPromotionResponse
    {
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = null!;
        public decimal ProductPrice { get; set; }
        public ProductStatus ProductStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<string> UrlImage { get; set; } = new();
        public bool IsActive { get; set; }
    }
}
