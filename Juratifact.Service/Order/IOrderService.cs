namespace Juratifact.Service.Order;

public interface IOrderService
{
    public Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CreateOrderRequest request);
    public Task<Response.GetOrderStatusResponse> GetStatusOrder(Guid id);
}