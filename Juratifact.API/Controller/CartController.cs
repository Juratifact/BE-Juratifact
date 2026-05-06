using Juratifact.Service.Cart;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class CartController: ControllerBase
{
    private readonly ICartService _cartService;
    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost("api/add-product-to-cart")]
    public async Task<IActionResult> AddProduct(Guid userId, Request.CartRequest request)
    {
        var result = await _cartService.AddProduct(userId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully", HttpContext.TraceIdentifier));
    }

    [HttpDelete("api/remove-product-from-cart")]
    public async Task<IActionResult> RemoveProduct(Guid userId, Guid cartDetailId)
    {
        var result1 = await _cartService.RemoveProduct(userId, cartDetailId);
        return Ok(ApiResponseFactory.SuccessResponse(result1, "successfully", HttpContext.TraceIdentifier));
    }
}