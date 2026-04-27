using Juratifact.Repository.Enum;

namespace Juratifact.Service.Report;

public class Request
{
    public class ReportRequest
    {
        public Guid Id { get; set; }
        public required string Reason { get; set; }
        public string? Description { get; set; }
        public Guid ProductId { get; set; }
    }
}