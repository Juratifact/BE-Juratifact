using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Otp: BaseEntity<Guid>,IAuditableEntity
{
    public required string Code { get; set; }
    public Enum OTPType { get; set; }
    public DateTimeOffset ExprireAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}