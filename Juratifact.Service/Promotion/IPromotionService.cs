namespace Juratifact.Service.Promotion;

public interface IPromotionService
{
    public Task<List<Response.PromotionPackageResponse>> GetPromotionPackages();
    public Task<string> CreatePromotion(Request.PromotionRequest request);
    public Task<Response.SubscribeResponse> SubscribeByPackageId(Guid packageId);
    public Task<List<Response.PromotionSubscribeResponse>> GetSubscribedPromotions();
}