namespace Juratifact.Service.Cart;

public class Response
{
    public class GetCartResponse
    {
        public Guid ProductId { get; set; }
        public required string Title { get; set; }
        public required string Condition { get; set; }
        public decimal Price { get; set; }
        public Guid SellerId { get; set; }
        public string? UserName { get; set; }
        public required string SellerName { get; set; }
    }
}