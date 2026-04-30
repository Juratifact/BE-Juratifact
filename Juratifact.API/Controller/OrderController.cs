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
    public async Task<IActionResult> CreateOrderProduct(Request.CreateOrderRequest request)
    {
        var result = await  _orderService.CreateOrderProduct(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Create order successfully", HttpContext.TraceIdentifier));
    }

    [HttpPost("/api/orders/{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var order = await _orderService.CancelOrder(id);
        return Ok(ApiResponseFactory.SuccessResponse(order, "Cancel order successfully", HttpContext.TraceIdentifier));
    }

    [HttpGet("api/orders/{id}/payment-info")]
    public async Task<IActionResult> GetPaymentInfo(Guid id)
    {
        var order2 = await _orderService.GetPaymentInfo(id);
        return Ok(ApiResponseFactory.SuccessResponse(order2, "Get payment info successfully", HttpContext.TraceIdentifier));
    }
    
}