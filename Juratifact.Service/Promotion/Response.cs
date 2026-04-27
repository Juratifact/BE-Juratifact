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
    
    public class SubscribeResponse
    {
        public required string QrUrl { get; set; }
    }
}