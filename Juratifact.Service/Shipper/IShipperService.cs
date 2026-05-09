using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.Shipper;

public interface IShipperService
{
    public Task<List<Response.ShipperResponse>> GetListOrder();
    public Task<string> AcceptOrder(Guid orderId, Guid shipperId);
    public Task<string> ConfirmPickupOrder(Guid orderId, Guid shipperId, IFormFile pod1Image);
    public Task<string> ConfirmDelivery(Guid orderId, Guid shipperId,  IFormFile pod2Image);  
    public Task<List<Response.ShipperActiveOrderResponse>> GetMyOrdersShipper(Guid shipperId);
    public Task<Response.ShipperActiveOrderResponse?> GetMyOrdersShipperByOrderId(Guid shipperId, Guid orderId);
}