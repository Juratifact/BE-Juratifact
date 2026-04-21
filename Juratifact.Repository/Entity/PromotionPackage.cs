using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class PromotionPackage: BaseEntity<Guid>, IAuditableEntity
{
    public required string PackageName { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    
    public int DurationDay { get; set; }
    public string? Type { get; set; }
    public int PostingDayCount { get; set; }
    
    public ICollection<UsePromotionSubscription>  UsePromotionSubscriptions { get; set; } = new List<UsePromotionSubscription>();
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}