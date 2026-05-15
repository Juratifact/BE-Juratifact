using Juratifact.Repository.Enum;

namespace Juratifact.Service.Dispute;

public class Response
{
    public class DisputeResponse
    {
        public Guid DisputeId { get; set; }
        public Guid OrderId { get; set; }
        public Guid? SellerOrderId { get; set; }
        public Guid BuyerId { get; set; }
        public string Reason { get; set; } = null!;
        public DisputeStatus Status { get; set; }
        public DisputeResolution Resolution { get; set; }
        public string? AdminNote { get; set; }
        public Guid? ResolvedByAdminId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
