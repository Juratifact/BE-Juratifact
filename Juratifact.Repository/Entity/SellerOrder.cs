using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class SellerOrder : BaseEntity<Guid>, IAuditableEntity
{
    public required string Code { get; set; }

    public decimal SubtotalPrice { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal SellerReceivableAmount { get; set; }

    public OrderStatus Status { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = default!;

    public Guid SellerId { get; set; }
    public User Seller { get; set; } = default!;

    public Guid? ShipperId { get; set; }
    public User? Shipper { get; set; }

    public string? ShipperPod1Url { get; set; }
    public string? ShipperPod2Url { get; set; }
    public DateTimeOffset? PickupAt { get; set; }
    public DateTimeOffset? DeliveryAt { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
    public ICollection<SellerReview> SellerReviews { get; set; } = new List<SellerReview>();

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
