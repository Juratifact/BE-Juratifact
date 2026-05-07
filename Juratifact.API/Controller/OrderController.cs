using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }
    
    // get để trên này cho dễ đọc nha -> ai "đọc comment xong thì xóa" nhen
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("all-orders")]
    public async Task<IActionResult> GetAllOrder()
    {
        var result = await _orderService.GetAllOrders();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get all order successfully", HttpContext.TraceIdentifier));
    }
    
    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatusOrder(Guid id)
    {
        var result = await _orderService.GetStatusOrder(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get status order successfully", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateOrderProduct(Request.CheckoutRequest request)
    {
        var result = await  _orderService.CreateOrderProduct(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Create order successfully", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("{orderId}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid orderId)
    {
        var result = await _orderService.ConfirmReceipt(orderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Confirm receipt order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder([FromRoute] Guid orderId, [FromBody] Request.CancelOrderRequest request)
    {
        var result = await _orderService.CancelOrder(orderId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Cancel order successfully", HttpContext.TraceIdentifier));
    }
    

}