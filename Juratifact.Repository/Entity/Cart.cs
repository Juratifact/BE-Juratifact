using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Cart: BaseEntity<Guid>,IAuditableEntity
{
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
