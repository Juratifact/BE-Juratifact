namespace Juratifact.Service.Order;

public class Response
{
    public class CreateOrderResponse
    {
        public Guid OrderId { get; set; }
        public required string ReferenceCode { get; set; }
        public required string QrUrl { get; set; }
    }
}