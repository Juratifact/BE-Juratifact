using Juratifact.Repository.Enum;

namespace Juratifact.Service.SellerOrders;

public class SellerOrderResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid ParentOrderId { get; set; }
    public string ParentOrderCode { get; set; } = string.Empty;

    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public string? ShippingVietMapRefId { get; set; }
    public double? ShippingLatitude { get; set; }
    public double? ShippingLongitude { get; set; }

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerPhone { get; set; }
    public string? SellerAddress { get; set; }
    public string? SellerVietMapRefId { get; set; }
    public string? SellerVietMapDisplay { get; set; }
    public double? SellerLatitude { get; set; }
    public double? SellerLongitude { get; set; }

    public Guid? ShipperId { get; set; }
    public string? ShipperName { get; set; }

    public decimal SubtotalPrice { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal SellerReceivableAmount { get; set; }

    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;

    public string? ShipperPod1Url { get; set; }
    public string? ShipperPod2Url { get; set; }
    public DateTimeOffset? PickupAt { get; set; }
    public DateTimeOffset? DeliveryAt { get; set; }
    public string? CancelReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public List<SellerOrderItemResponse> Items { get; set; } = new();
}

public class SellerOrderItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class SellerOrderTransactionResponse
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? SellerOrderId { get; set; }
    public Guid? WalletId { get; set; }
    public decimal Amount { get; set; }
    public decimal? FeeAmount { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TransactionType TransactionType { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
