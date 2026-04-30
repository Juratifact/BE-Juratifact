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
    }
    public class PromotionResponse
    {
        public int UniqueUsers { get; set; }
        public decimal TotalRevenue { get; set; }
    
    public class SubscribeResponse
    {
        public required string QrUrl { get; set; }
    }

    public class PromotionSubscribeResponse
    {
        public Guid PromotionPackageId { get; set; }
        public string PromotionPackageName { get; set; } = null!;
        public decimal Price { get; set; }
        
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
}