using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class SellerReview: BaseEntity<Guid>,IAuditableEntity
{
    public required int Rating { get; set; }
    public string? Comment { get; set; }
    
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}