namespace Juratifact.Service.Order;

public interface IOrderService
{
    public Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CheckoutRequest request);
    public Task<Response.GetOrderStatusResponse> GetStatusOrder(Guid id);
    public Task<List<Response.GetAllOrderResponse>>  GetAllOrders();
    public Task<string> ConfirmReceipt(Guid orderId);
    public Task<string> CancelOrder(Guid orderId, Request.CancelOrderRequest request);
    public Task<List<Response.GetMyOrderResponse>> GetMyOrder();
    public Task<Response.ProductListResponse> GetProductbyOrderId(Guid orderId, Guid productId);
}