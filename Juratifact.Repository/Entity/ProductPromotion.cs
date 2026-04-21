using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class ProductPromotion : BaseEntity<Guid>, IAuditableEntity
{
    public bool IsActive { get; set; } = false;
    
    public Guid UsePromotionSubscriptionId { get; set; }
    public UsePromotionSubscription UsePromotionSubscription { get; set; }
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}