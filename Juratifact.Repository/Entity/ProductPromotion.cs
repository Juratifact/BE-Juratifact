using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class ProductPromotion : BaseEntity<Guid>, IAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    
    public Guid PackageId { get; set; }
    public PromotionPackage Package { get; set; }
    
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; }
    
    public required string PaymentCode { get; set; }
    public required string PaymentStatus { get; set; }
    
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}