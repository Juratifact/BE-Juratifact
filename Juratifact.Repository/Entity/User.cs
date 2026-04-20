using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class User: BaseEntity<Guid>,IAuditableEntity
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string? ProfilePicture { get; set; }
    public required string Salt { get; set; }
    public bool isVerify { get; set; } = false;
    public decimal TrustScore { get; set; } = 0;
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}