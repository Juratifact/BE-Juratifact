using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class UserRole: BaseEntity<Guid>,IAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public Guid RoleId { get; set; }
    public Role Role { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}