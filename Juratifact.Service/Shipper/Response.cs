using Juratifact.Repository.Enum;

namespace Juratifact.Service.Shipper;

public class Response
{
    public class ShipperResponse
    {
        public Guid OrderId { get; set; }
        public Guid SellerOrderId { get; set; }
        public Guid ParentOrderId { get; set; }
        public string? Code { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public Guid SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? AddressSeller { get; set; } 
        public string? AddressBuyer { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderDetailDto> Items { get; set; } = new();

    }
    
    public class ShipperActiveOrderResponse
    {
        // --- IDENTITY ---
        public Guid OrderId { get; set; }
        public Guid ParentOrderId { get; set; }
        public Guid SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? SellerPhone { get; set; }
        public string? SellerAddress { get; set; }
        // public string? SellerAddressVietMap { get; set; }
        public string Name { get; set; }
        public OrderStatus Status { get; set; }

        // --- FINANCIAL ---
        public decimal TotalPrice { get; set; }
        public decimal ShippingFee { get; set; }
        public string PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
                
        // --- ADDRESS ---
        public string? ShippingAddress { get; set; }

        // --- CUSTOMER INFO (join từ User) ---
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        // --- TIMESTAMPS ---
        public DateTimeOffset? PickupAt { get; set; }
        public DateTimeOffset? DeliveryAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }

        // --- PROOF OF DELIVERY ---
        public string? ShipperPod1Url { get; set; }
        public string? ShipperPod2Url { get; set; }

        // --- ORDER ITEMS (từ OrderDetails) ---
        public List<OrderDetailDto> Items { get; set; }
    }

    public class OrderDetailDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } 
        public decimal Price { get; set; }
        public List<string> ImageUrl { get; set; } = new();
    }
}
