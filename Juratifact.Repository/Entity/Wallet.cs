using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Wallet: BaseEntity<Guid>,IAuditableEntity
{
    public decimal Balance  { get; set; } 
    public decimal PendingBalance { get; set; }
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}