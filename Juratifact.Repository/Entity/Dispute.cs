using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Dispute : BaseEntity<Guid>, IAuditableEntity
{
    // Liên kết với Order (Quan hệ 1:n )
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    
    // Liên kết với Buyer (Người tạo khiến nại)
    public Guid BuyerId { get; set; }
    public User Buyer { get; set; }
    
    // Liên kết với Admin/Support (Người xử lý - có thể null khi mới tạo)
    public Guid? ResolvedByAdminId { get; set; }
    public User? ResolvedByAdmin { get; set; }
    
    // Các trường dữ liệu khác
    public string Reason { get; set; }
    public DisputeStatus Status { get; set; }
    public DisputeResolution Resolution { get; set; }
    public string? AdminNote { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}