namespace Juratifact.Service.Order;

public interface IOrderService
{
    public Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CreateOrderRequest request);
}