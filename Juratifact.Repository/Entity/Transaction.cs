using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Transaction: BaseEntity<Guid>,IAuditableEntity
{
    public required string SepayId { get; set; }
    public decimal Amount { get; set; }
    public Enum? TransactionType { get; set; }
    public required string ExternalTransactionId { get; set; }
    public required string Description { get; set; }
    public required string ReferenceCode { get; set; }
    public decimal FeeAmount { get; set; }
    public Enum? Status { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
