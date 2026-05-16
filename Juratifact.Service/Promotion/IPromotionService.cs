namespace Juratifact.Service.Promotion;

public interface IPromotionService
{
    public Task<Base.Response.PageResult<Response.PromotionPackageResponse>> GetPromotionPackages(int pageSize, int pageIndex);
    public Task<string> CreatePromotion(Request.PromotionRequest request);
    public Task<string> SoftDeletePromotionPackage(Guid packageId);
    public Task<Response.SubscribeResponse> SubscribeByPackageId(Guid packageId);
    public Task<List<Response.PromotionSubscribeResponse>> GetSubscribedPromotions();
    public Task<string> ApplyProductPromotion(Request.ProductPromotionRequest request);
    public Task<string> ChangeStatusPromotion(Guid id);
    public Task<List<Response.GetProductPromotionResponse>> GetProductPromotion();
    public Task<List<Response.PromotionProductResponse>> GetProductsByPromotionPackageId(Guid promotionPackageId);
}
