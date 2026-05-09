namespace Juratifact.Service.Cart;

public class Response
{
    public class GetCartResponse
    {
        public Guid CartDetailId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; }
        // Thay đổi từ string sang List<string>
        public List<string> ProductImageUrls { get; set; } = new List<string>();
        public List<string?> ProductVideoUrls { get; set; } = new List<string?>(); 
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; }
        public Guid SellerId { get; set; }
        public string SellerName { get; set; }
        public DateTimeOffset AddedAt { get; set; }
    }
}