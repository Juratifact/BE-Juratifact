using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class IdentityDocument: BaseEntity<Guid>,IAuditableEntity
{
    public required string IdCardFrontUrl { get; set; }
    public required string IdCardBackUrl { get; set; }
    public DateTimeOffset ExpireAt { get; set; }
    public Enum? Status { get; set; }
    public required string VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public required string Note { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}