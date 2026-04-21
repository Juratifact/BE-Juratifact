using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Wallet: BaseEntity<Guid>,IAuditableEntity
{
    public required decimal Balance  { get; set; } 
    public required decimal PendingBalance { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public ICollection<Transaction> Transactions { get; set; } =  new List<Transaction>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}