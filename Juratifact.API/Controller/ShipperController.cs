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
    [HttpGet("api/shipper/available-orders")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var result = await _shipperService.GetListOrder();
        return Ok(ApiResponseFactory.SuccessResponse(result,HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("api/shipper/accept-order")]
    public async Task<IActionResult> AcceptOrder(Guid orderId, Guid shipperId)
    {
        var result = await _shipperService.AcceptOrder(orderId, shipperId);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully",HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("api/shipment/confirm-pickup")]
    public async Task<IActionResult> ConfirmPickup(Guid orderId, Guid shipperId, IFormFile pod1Image)
    {
        var result = await _shipperService.ConfirmPickupOrder(orderId, shipperId, pod1Image);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully",HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.ShipperPolicy)]
    [HttpPost("api/shipment/confirm-delivery")]
    public async Task<IActionResult> ConfirmDelivery(Guid orderId, Guid shipperId, IFormFile pod2Image)
    {
        var result = await _shipperService.ConfirmDelivery(orderId, shipperId, pod2Image);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully",HttpContext.TraceIdentifier));
    }
}