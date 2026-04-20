using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class OrderDetail : BaseEntity<Guid>, IAuditableEntity
{
    public decimal Price { get; set; }
    
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}