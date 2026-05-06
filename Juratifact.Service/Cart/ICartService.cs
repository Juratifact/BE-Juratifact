namespace Juratifact.Service.Cart;

public interface ICartService
{
    public Task<string> AddProduct(Guid userId,Request.CartRequest request);
    public Task<string> RemoveProduct(Guid userId, Guid cartDetailId);
    public Task<List<Response.GetCartResponse>> GetMyCart();
}