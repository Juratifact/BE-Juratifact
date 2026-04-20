using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Otp: BaseEntity<Guid>,IAuditableEntity
{
    public string Code { get; set; }
    public OtpStatus OTPType { get; set; }
    public DateTimeOffset ExprireAt { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}