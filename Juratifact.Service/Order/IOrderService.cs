namespace Juratifact.Service.Order;

public interface IOrderService
{
    public Task<Response.CreateOrderResponse> CreateOrderProduct(Request.CheckoutRequest request);
    public Task<Response.GetOrderStatusResponse> GetStatusOrder(Guid id);
    public Task<Base.Response.PageResult<Response.GetAllOrderResponse>>  GetAllOrders(int pageSize, int pageIndex);
    public Task<string> ConfirmReceipt(Guid orderId);
    public Task<string> ConfirmSellerOrderReceipt(Guid sellerOrderId);
    public Task<string> CancelOrder(Guid orderId, Request.CancelOrderRequest request);
    public Task<string> CancelCheckout(Guid orderId);
    public Task<Base.Response.PageResult<Response.GetMyOrderResponse>> GetMyOrder(int pageSize, int pageIndex);
    public Task<Response.ProductListResponse> GetProductbyOrderId(Guid orderId, Guid productId);
    public Task<string> UpdateShippingAddress(Guid orderId, Request.UpdateShippingAddressRequest request);
}
