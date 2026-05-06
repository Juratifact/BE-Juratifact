using Juratifact.Repository.Enum;

namespace Juratifact.Service.Dispute;

public interface IDisputeService
{
    public Task<string> CreateDispute(Guid orderId, Request.CreateDisputeRequest request);
    public Task<Base.Response.PageResult<Response.DisputeResponse>> GetMyDispute(int pageSize, int pageIndex);
    public Task<string> ResolveDispute(Guid disputeId, Request.ResolveDisputeRequest request);
    public Task<string> CancelDispute(Guid disputeId);
    
    public Task<Base.Response.PageResult<Response.DisputeResponse>> GetDisputes(DisputeStatus? status, int pageSize, int pageIndex);
    
    public Task<string> AssignDispute(Guid disputeId, Request.AssignDisputeRequest request);
}