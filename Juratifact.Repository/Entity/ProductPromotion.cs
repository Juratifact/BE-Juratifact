using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class ProductPromotion : BaseEntity<Guid>, IAuditableEntity
{
    public bool IsActive { get; set; } = false;
    public DateTimeOffset? ActiveAt { get; set; } 
    public DateTimeOffset? ExpiresAt { get; set; }
    
    public Guid UsePromotionSubscriptionId { get; set; }
    public UserPromotionSubscription UserPromotionSubscription { get; set; }
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}