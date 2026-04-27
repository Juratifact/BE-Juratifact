namespace Juratifact.Service.Sepay;

public class Request
{
    public class SepayWebhookDto
    {
        public long Id { get; set; }           
        public string Content { get; set; }    
        public decimal TransferAmount { get; set; }
        public string ReferenceCode { get; set; } 
        public string TransactionDate { get; set; }
        public string Gateway { get; set; }
    }
}