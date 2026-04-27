namespace Juratifact.Service.Report;

public interface IReportService
{
    public Task<string> CreateReport(Request.ReportRequest request);
    public Task<Base.Response.PageResult<Response.ReportResponse>> GetReport(string? searchTerm,int pageSize, int pageIndex);
    public Task<string> ApproveReport(Guid id);
    public Task<string> RejectReport(Guid id);
}