namespace Juratifact.Service.Order;

public class Request
{
    public class CheckoutRequest
    {
        public string? ShippingAddress { get; set; }
        public string? VietMapRefId { get; set; }
        public List<Guid>? CartDetailIds { get; set; }
    }
    
    public class CancelOrderRequest
    {
        public required string Reason { get; set; }
    }

    public class UpdateShippingAddressRequest
    {
        public required string NewAddress { get; set; }
    }
}
