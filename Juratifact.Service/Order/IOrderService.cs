namespace Juratifact.Service.Order;

public interface IOrderService
{
    public Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CreateOrderRequest request);
    public Task<Response.GetOrderStatusResponse> GetStatusOrder(Guid id);
    public Task<List<Response.GetAllOrderResponse>>  GetAllOrders();
    
    public Task<string> CancelOrder(Guid id);
    public Task<Response.CreateOrderResponse> GetPaymentInfo(Guid id);
}