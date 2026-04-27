using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Transaction: BaseEntity<Guid>,IAuditableEntity
{
    public required string SepayId { get; set; }
    public decimal Amount { get; set; }
    public required string ExternalTransactionId { get; set; }
    public required string Description { get; set; }
    public required string ReferenceCode { get; set; }
    public decimal FeeAmount { get; set; }
    
    public TransactionType TransactionType { get; set; }
    public TransactionStatus Status { get; set; }
    
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; }
    
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    
    public Guid UserPromotionSubscriptionId { get; set; }
    public UserPromotionSubscription UserPromotionSubscription { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
