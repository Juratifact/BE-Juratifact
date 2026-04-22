using Juratifact.Repository.Enum;

namespace Juratifact.Service.Report;

public class Response
{
    public class ReportResponse
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = "";
        public string? Description { get; set; }
        public ReportStatus Status { get; set; }
        public Guid ProductId { get; set; }
        public Guid UserId { get; set; }
    }
}