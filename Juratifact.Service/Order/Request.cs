namespace Juratifact.Service.Order;

public class Request
{
    public class CreateOrderRequest
    {
        public string Name { get; set; }
        public List<ProductOrderRequest> Products { get; set; }
    }
    
    public class ProductOrderRequest
    {
        public Guid ProductId { get; set; }
    }
}