using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class ProductMedia : BaseEntity<Guid>, IAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    
    public required string ImageUrl { get; set; }
    public string? Video { get; set; } // Optional
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}