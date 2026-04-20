using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class PromotionPackage: BaseEntity<Guid>, IAuditableEntity
{
    public required string PackageName { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    
    public required int DurationDay { get; set; }
    public string? Type { get; set; }
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}