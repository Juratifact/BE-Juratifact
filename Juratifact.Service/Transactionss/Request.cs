using Juratifact.Repository.Enum;

namespace Juratifact.Service.Transactionss;

public class Request
{
    public class TransactionRequest
    {
        public TransactionType? TransactionType { get; set; }
        public TransactionStatus? Status { get; set; }
    }
}