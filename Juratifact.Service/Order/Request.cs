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
        public string? NewAddress { get; set; }
        // Optional: VietMap reference id. If provided the service will resolve and store
        // the full place display and coordinates instead of using NewAddress directly.
        public string? VietMapRefId { get; set; }
    }
}
