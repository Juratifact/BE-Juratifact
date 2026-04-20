using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class CartDetail: BaseEntity<Guid>,IAuditableEntity
{
    public int Quantity { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}