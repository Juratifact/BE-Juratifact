using Juratifact.Repository.Enum;

namespace Juratifact.Service.Order;

public class Response
{
    public class CreateOrderResponse
    {
        public Guid OrderId { get; set; }
        public List<Guid> SellerOrderIds { get; set; } = new();
        public required string ReferenceCode { get; set; }
        public required string QrUrl { get; set; }
    }
    
    public class GetOrderStatusResponse
    {
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class GetAllOrderResponse
    {
        public Guid OrderId { get; set; }
        public string Name { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class GetMyOrderResponse : GetAllOrderResponse
    {
        public Guid ProductId { get; set; }
        public required string Title { get; set; }
        public required string Condition { get; set; }
        public decimal Price { get; set; }
        public Guid? SellerOrderId { get; set; }
        public OrderStatus ParentOrderStatus { get; set; }
        public bool CanConfirmReceipt { get; set; }
        public Guid SellerId { get; set; }
        public string? UserName { get; set; }
        public required string SellerName { get; set; }
    }
    
    public class ProductListResponse
    {
        public Guid ProductId { get; set; }
        public Guid SellerId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Condition { get; set; }
        public decimal? Price { get; set; }
        public List<string> ImageUrl { get; set; }
        public List<string> Video { get; set; }
    }
}
