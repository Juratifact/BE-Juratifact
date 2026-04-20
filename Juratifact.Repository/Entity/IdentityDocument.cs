using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class IdentityDocument: BaseEntity<Guid>,IAuditableEntity
{
    public required string IdCardFrontUrl { get; set; }
    public required string IdCardBackUrl { get; set; }
    public DateTimeOffset ExpireAt { get; set; }
    public IdentityStatus Status { get; set; }
    public required string VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public required string Note { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}