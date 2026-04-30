namespace Juratifact.Service.Promotion;

public class Request
{
    public class PromotionRequest
    {
        public required string PackageName { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        
        public int? MaxProductCount { get; set; }
        public int? PromotionDaysPerSlot { get; set; }
        public DateTimeOffset? AvailableFrom { get; set; }
        public DateTimeOffset? AvailableTo { get; set; }
        public int? UsageLimitDays { get; set; }
    }
    
    public class ProductPromotionRequest
    {
        public Guid ProductId { get; set; }
        public Guid PromotionPackageId { get; set; }
    }
}