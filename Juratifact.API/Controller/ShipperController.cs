using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Shipper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class ShipperController:ControllerBase
{
    private readonly IShipperService _shipperService;

    public ShipperController(IShipperService shipperService)
    {
        _shipperService = shipperService;
    }

    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpGet("available-orders")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var result = await _shipperService.GetListOrder();
        return Ok(ApiResponseFactory.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("accept-order")]
    public async Task<IActionResult> AcceptOrder(Guid orderId, Guid shipperId)
    {
        var result = await _shipperService.AcceptOrder(orderId, shipperId);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully",HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpGet("{shipperId}/my-orders")]
    public async Task<IActionResult> GetMyOrdersShipper(Guid shipperId, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 1)
    {
        var result = await _shipperService.GetMyOrdersShipper(shipperId, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpGet("my-ordersByOrderID")]
    public async Task<IActionResult> GetMyOrdersShipperByOrderId(Guid shipperId, Guid orderId)
    {
        var result = await _shipperService.GetMyOrdersShipperByOrderId(shipperId, orderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("confirm-pickup")]
    public async Task<IActionResult> ConfirmPickup(Guid orderId, Guid shipperId, IFormFile pod1Image)
    {
        var result = await _shipperService.ConfirmPickupOrder(orderId, shipperId, pod1Image);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully",HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("confirm-delivery")]
    public async Task<IActionResult> ConfirmDelivery(Guid orderId, Guid shipperId, IFormFile pod2Image)
    {
        var result = await _shipperService.ConfirmDelivery(orderId, shipperId, pod2Image);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully",HttpContext.TraceIdentifier));
    }
}