using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Order: BaseEntity<Guid>,IAuditableEntity
{
    public required string Name { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal ShippingPee { get; set; } //
    public string? ShipperPod1Url { get; set; }
    public string? ShipperPod2Url { get; set; }
    public string PaymentMethod { get; set; } = "Banking";
    
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; } 
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public SellerReview SellerReview { get; set; }
    
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}