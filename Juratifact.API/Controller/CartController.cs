using Juratifact.API.Extensions;
using Juratifact.Service.Cart;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Authorize(Policy = JwtExtensions.BuyerPolicy)]
[ApiController]
[Route("api/carts")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyCart(int pageIndex = 1, int pageSize = 10)
    {
        var cart = await _cartService.GetMyCart(pageIndex, pageSize);
        return Ok(ApiResponseFactory.SuccessResponse(cart, "Get my cart successfully", HttpContext.TraceIdentifier));
    }

    [HttpPost("{userId:guid}/items")]
    public async Task<IActionResult> AddProduct([FromRoute] Guid userId, Request.CartRequest request)
    {
        var result = await _cartService.AddProduct(userId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "successfully", HttpContext.TraceIdentifier));
    }

    [HttpDelete("{userId:guid}/items/{productId:guid}")]
    public async Task<IActionResult> RemoveProduct([FromRoute] Guid userId, [FromRoute] Guid productId)
    {
        var result1 = await _cartService.RemoveProduct(userId, productId);
        return Ok(ApiResponseFactory.SuccessResponse(result1, "successfully", HttpContext.TraceIdentifier));
    }
}
