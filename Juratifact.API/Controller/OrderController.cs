using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet]
    public async Task<IActionResult> GetAllOrder(int pageSize = 10, int pageIndex = 1)
    {
        var result = await _orderService.GetAllOrders(pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get all order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyOrder(int pageSize = 10, int pageIndex = 1)
    {
        var result = await _orderService.GetMyOrder(pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get my order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatusOrder(Guid id)
    {
        var result = await _orderService.GetStatusOrder(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get status order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost]
    public async Task<IActionResult> CreateOrderProduct(Request.CheckoutRequest request)
    {
        var result = await _orderService.CreateOrderProduct(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Create order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("{orderId:guid}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(Guid orderId)
    {
        var result = await _orderService.ConfirmReceipt(orderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Confirm receipt order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder([FromRoute] Guid orderId, [FromBody] Request.CancelOrderRequest request)
    {
        var result = await _orderService.CancelOrder(orderId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Cancel order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("{orderId:guid}/cancel-checkout")]
    public async Task<IActionResult> CancelCheckout(Guid orderId)
    {
        var result = await _orderService.CancelCheckout(orderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Cancel checkout successfully", HttpContext.TraceIdentifier));
    }

    [HttpGet("{orderId:guid}/products/{productId:guid}")]
    public async Task<IActionResult> GetProductbyOrderId(Guid orderId, Guid productId)
    {
        var result = await _orderService.GetProductbyOrderId(orderId, productId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("{orderId:guid}/shipping-address")]
    public async Task<IActionResult> UpdateShippingAddress([FromRoute] Guid orderId,
        [FromBody] Request.UpdateShippingAddressRequest request)
    {
        var result = await _orderService.UpdateShippingAddress(orderId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Update shipping address successfully", HttpContext.TraceIdentifier));
    }
}
