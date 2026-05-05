using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Notification: BaseEntity<Guid>,IAuditableEntity
{
    public string Content { get; set; } = string.Empty;
    public string Title { get; set; } =  string.Empty;
    public NotificationType Type { get; set; }
    public string? RedirectUrl { get; set; }
    public bool IsRead { get; set; } = false;
    
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } =  DateTimeOffset.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
}