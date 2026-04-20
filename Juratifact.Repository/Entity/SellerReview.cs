using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class SellerReview: BaseEntity<Guid>,IAuditableEntity
{
    public int Rating { get; set; }
    public string Comment { get; set; }
    
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public User User { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}