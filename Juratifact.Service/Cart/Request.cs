namespace Juratifact.Service.Cart;

public class Request
{
    public class CartRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}