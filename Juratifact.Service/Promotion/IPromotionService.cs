namespace Juratifact.Service.Promotion;

public interface IPromotionService
{
    public Task<string> CreatePromotion(Request.PromotionRequest request);
}