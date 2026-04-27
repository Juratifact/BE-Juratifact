namespace Juratifact.Service.Promotion;

public interface IPromotionService
{
    public Task<List<Response.PromotionPackageResponse>> GetPromotionPackages();
}