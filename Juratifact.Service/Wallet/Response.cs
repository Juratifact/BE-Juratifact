namespace Juratifact.Service.Wallet;

public class Response
{
    public class WalletResponse
    {
        public decimal? Balance { get; set; } 
        public decimal PendingBalance { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}