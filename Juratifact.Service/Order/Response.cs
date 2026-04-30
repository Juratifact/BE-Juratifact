using Juratifact.Repository.Enum;

namespace Juratifact.Service.Order;

public class Response
{
    public class CreateOrderResponse
    {
        public Guid OrderId { get; set; }
        public required string ReferenceCode { get; set; }
        public required string QrUrl { get; set; }
        public decimal Amount { get; set; }
    }
    
    public class GetOrderStatusResponse
    {
        public OrderStatus Status { get; set; }
    }

    public class GetAllOrderResponse
    {
        public Guid OrderId { get; set; }
        public string Name { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }
}