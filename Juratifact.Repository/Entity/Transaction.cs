using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Transaction: BaseEntity<Guid>,IAuditableEntity
{
    public string SepayId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public string ExternalTransactionId { get; set; }
    public string Description { get; set; }
    public string ReferenceCode { get; set; }
    public decimal FeeAmount { get; set; }
    public TransactionStatus Status { get; set; }
    
    public Guid? WalletId { get; set; }
    public Wallet Wallet { get; set; }
    
    public Guid? OrderId { get; set; }
    public Order Order { get; set; }
    
    public ICollection<PromotionPackage> PromotionPackages { get; set; } = new List<PromotionPackage>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
