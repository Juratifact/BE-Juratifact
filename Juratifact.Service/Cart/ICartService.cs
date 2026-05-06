namespace Juratifact.Service.Cart;

public interface ICartService
{
    public Task<List<Response.GetCartResponse>> GetMyCart();
}