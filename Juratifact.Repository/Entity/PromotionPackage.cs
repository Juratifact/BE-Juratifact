using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class PromotionPackage: BaseEntity<Guid>, IAuditableEntity
{
    public required string PackageName { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    
    public int? MaxProductCount { get; set; }
    public int? PromotionDaysPerSlot { get; set; }
    public DateTimeOffset? AvailableFrom { get; set; }
    public DateTimeOffset? AvailableTo { get; set; }
    public int? UsageLimitDays  { get; set; }
    
    public ICollection<UserPromotionSubscription>  UsePromotionSubscriptions { get; set; } = new List<UserPromotionSubscription>();
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}