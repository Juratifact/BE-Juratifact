using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class ProductCategory : BaseEntity<Guid>, IAuditableEntity
{
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}