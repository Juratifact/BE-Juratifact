using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class Order: BaseEntity<Guid>,IAuditableEntity
{
    public required string Name { get; set; }
    public decimal TotalPrice { get; set; }
    public Enum? Status { get; set; }
    public required string ShipperPod1Url { get; set; }
    public required string ShipperPod2Url { get; set; }
    public string PaymentMethod { get; set; } = "Banking";
    public Enum? PaymentStatus { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; } 
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}