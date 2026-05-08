using Juratifact.Repository.Enum;

namespace Juratifact.Service.Order;

public class Response
{
    public class CreateOrderResponse
    {
        public Guid OrderId { get; set; }
        // public List<Guid> OrderIds { get; set; } = new();
        public required string ReferenceCode { get; set; }
        public required string QrUrl { get; set; }
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

    public class GetMyOrderResponse : GetAllOrderResponse
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