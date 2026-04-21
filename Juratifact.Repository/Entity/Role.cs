using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Role: BaseEntity<Guid>,IAuditableEntity
{
    public required string Name { get; set; }
    
    public ICollection<UserRole> UserRoles { get; set; } =  new List<UserRole>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}