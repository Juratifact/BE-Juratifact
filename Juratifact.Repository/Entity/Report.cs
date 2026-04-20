using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Report: BaseEntity<Guid>,IAuditableEntity
{
    public required string Reason { get; set; }
    public required string Description { get; set; }
    public ReportStatus Status { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}