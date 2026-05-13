using Juratifact.Repository.Enum;

namespace Juratifact.Service.Transactionss;

public class Response
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public string? SepayId { get; set; }
        public decimal Amount { get; set; }
        public string? ExternalTransactionId { get; set; }
        public string? Description { get; set; }
        public required string ReferenceCode { get; set; }
        public decimal? FeeAmount { get; set; }
    
        public TransactionType TransactionType { get; set; }
        public TransactionStatus? Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}