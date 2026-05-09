namespace Juratifact.Service.Cart;

public interface ICartService
{
    public Task<string> AddProduct(Guid userId,Request.CartRequest request);
    public Task<string> RemoveProduct(Guid userId, Guid productId);
    public Task<Base.Response.PageResult<Response.GetCartResponse>> GetMyCart(int pageIndex, int pageSize);
}