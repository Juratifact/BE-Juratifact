using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Report: BaseEntity<Guid>,IAuditableEntity
{
    public required string Reason { get; set; }
    public required string Description { get; set; }
    public Enum? Status { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}