using Juratifact.API.Extensions;
using Juratifact.Service.Cart;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Authorize(Policy = JwtExtensions.BuyerPolicy)]
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("my-cart")]
    public async Task<IActionResult> GetMyCart()
    {
        var cart = await _cartService.GetMyCart();
        return Ok(ApiResponseFactory.SuccessResponse(cart, "Get my cart successfully", HttpContext.TraceIdentifier));
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