using Juratifact.API.Extensions;
using Juratifact.Service.Cart;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("my-cart")]
    public async Task<IActionResult> GetMyCart()
    {
        var cart = await _cartService.GetMyCart();
        return Ok(ApiResponseFactory.SuccessResponse(cart, "Get my cart successfully", HttpContext.TraceIdentifier));
    }
    
}