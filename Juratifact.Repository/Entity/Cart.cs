using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Cart: BaseEntity<Guid>,IAuditableEntity
{
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
