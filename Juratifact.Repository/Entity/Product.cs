using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Product : BaseEntity<Guid>, IAuditableEntity
{
    public required Guid SellerId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public Enum? Status { get; set; } // trạng thái tin đăng lên
    public required string Condition { get; set; } // New, Used, Refurbished, etc.
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}