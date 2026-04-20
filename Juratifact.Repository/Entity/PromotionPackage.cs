using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class PromotionPackage: BaseEntity<Guid>, IAuditableEntity
{
    public string PackageName { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    
    public int DurationDay { get; set; }
    public string? Type { get; set; }
    
    public ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}