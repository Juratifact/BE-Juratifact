using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class ProductComment  : BaseEntity<Guid>, IAuditableEntity
{
    public required string Content { get; set; } //question
    
    public Guid? ParentCommentId { get; set; } 
    public ProductComment? Parent { get; set; } //reply
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public ICollection<ProductComment> Children { get; set; } = new List<ProductComment>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}