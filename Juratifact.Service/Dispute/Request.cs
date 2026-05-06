using Juratifact.Repository.Enum;

namespace Juratifact.Service.Dispute;

public class Request
{
    public class CreateDisputeRequest
    {
        public required string Reason { get; set; }
    }

    public class ResolveDisputeRequest
    {
        public required DisputeResolution Result { get; set; } // Nhận RefundBuyer hoặc PaySeller
        public string? AdminNote { get; set; } // Lời nhắn/Lý do từ Admin
    }
    
    public class AssignDisputeRequest
    {
        public Guid? AssignedAdminId { get; set; }
    }
}