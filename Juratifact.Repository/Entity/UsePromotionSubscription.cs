using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class UsePromotionSubscription : BaseEntity<Guid>,IAuditableEntity
{
    public decimal TotalSlot { get; set; }
    public decimal UsedSlot { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public PaymentStatus paymentStatus { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public Guid PackageId { get; set; }
    public PromotionPackage  PromotionPackage { get; set; }
    
    public Transaction Transaction { get; set; }
    
    public ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}