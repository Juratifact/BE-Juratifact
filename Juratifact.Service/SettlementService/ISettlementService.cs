namespace Juratifact.Service.SettlementService;

public interface ISettlementService
{
    public Task<bool> ProcessSettlementAsync(Guid orderId);
}