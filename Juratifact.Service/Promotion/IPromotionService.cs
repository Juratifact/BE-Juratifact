namespace Juratifact.Service.Promotion;

public interface IPromotionService
{
    public Task<List<Response.PromotionPackageResponse>> GetPromotionPackages();
    public Task<string> CreatePromotion(Request.PromotionRequest request);
    public Task<Response.PromotionResponse> GetSubscriptions();
    public Task<string> DeletePromotion(Guid id);
}