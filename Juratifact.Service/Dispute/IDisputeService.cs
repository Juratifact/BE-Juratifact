namespace Juratifact.Service.Dispute;

public interface IDisputeService
{
    public Task<string> CreateDispute(Guid orderId, Request.CreateDisputeRequest request);
    
    public Task<string> ResolveDispute(Guid disputeId, Request.ResolveDisputeRequest request);
}