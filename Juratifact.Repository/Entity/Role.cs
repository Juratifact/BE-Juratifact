using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Role: BaseEntity<Guid>,IAuditableEntity
{
    public required string Name { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}