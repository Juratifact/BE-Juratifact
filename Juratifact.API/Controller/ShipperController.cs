using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Shipper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/shippers")]
public class ShipperController : ControllerBase
{
    private readonly IShipperService _shipperService;

    public ShipperController(IShipperService shipperService)
    {
        _shipperService = shipperService;
    }

    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpGet("orders/available")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var result = await _shipperService.GetListOrder();
        return Ok(ApiResponseFactory.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("{shipperId:guid}/orders/{orderId:guid}/acceptance")]
    public async Task<IActionResult> AcceptOrder([FromRoute] Guid orderId, [FromRoute] Guid shipperId)
    {
        var result = await _shipperService.AcceptOrder(orderId, shipperId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpGet("{shipperId:guid}/orders")]
    public async Task<IActionResult> GetMyOrdersShipper(Guid shipperId, [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 1)
    {
        var result = await _shipperService.GetMyOrdersShipper(shipperId, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpGet("{shipperId:guid}/orders/{orderId:guid}")]
    public async Task<IActionResult> GetMyOrdersShipperByOrderId(Guid shipperId, Guid orderId)
    {
        var result = await _shipperService.GetMyOrdersShipperByOrderId(shipperId, orderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("{shipperId:guid}/orders/{orderId:guid}/pickup")]
    public async Task<IActionResult> ConfirmPickup([FromRoute] Guid orderId, [FromRoute] Guid shipperId, IFormFile pod1Image)
    {
        var result = await _shipperService.ConfirmPickupOrder(orderId, shipperId, pod1Image);
        return Ok(ApiResponseFactory.SuccessResponse(result, "successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("{shipperId:guid}/orders/{orderId:guid}/delivery")]
    public async Task<IActionResult> ConfirmDelivery([FromRoute] Guid orderId, [FromRoute] Guid shipperId, IFormFile pod2Image)
    {
        var result = await _shipperService.ConfirmDelivery(orderId, shipperId, pod2Image);
        return Ok(ApiResponseFactory.SuccessResponse(result, "successfully", HttpContext.TraceIdentifier));
    }
}
