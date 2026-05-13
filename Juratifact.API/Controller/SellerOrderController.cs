using Juratifact.API.Extensions;
using Juratifact.Repository.Enum;
using Juratifact.Service.Models;
using Juratifact.Service.SellerOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/seller-orders")]
public class SellerOrderController : ControllerBase
{
    private readonly ISellerOrderService _sellerOrderService;

    public SellerOrderController(ISellerOrderService sellerOrderService)
    {
        _sellerOrderService = sellerOrderService;
    }

    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMySellerOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _sellerOrderService.GetMySellerOrders(status, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get seller orders successfully", HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("{sellerOrderId:guid}")]
    public async Task<IActionResult> GetSellerOrderById([FromRoute] Guid sellerOrderId)
    {
        var result = await _sellerOrderService.GetSellerOrderById(sellerOrderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get seller order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetSellerOrdersByParentOrderId([FromRoute] Guid orderId)
    {
        var result = await _sellerOrderService.GetSellerOrdersByParentOrderId(orderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get seller orders by parent order successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.AdminOrSellerPolicy)]
    [HttpGet("{sellerOrderId:guid}/transactions")]
    public async Task<IActionResult> GetSellerOrderTransactions([FromRoute] Guid sellerOrderId)
    {
        var result = await _sellerOrderService.GetSellerOrderTransactions(sellerOrderId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get seller order transactions successfully", HttpContext.TraceIdentifier));
    }
}
