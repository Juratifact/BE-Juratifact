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
        public Repository.Entity.Product Product { get; set; }
        public Repository.Entity.User Reporter { get; set; }
    }
}