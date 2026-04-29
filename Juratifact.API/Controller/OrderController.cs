using Juratifact.Service.Models;
using Juratifact.Service.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Authorize]
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

    [HttpPost("/checkout")]
    public async Task<IActionResult> CreateOrderProduct(Request.CreateOrderRequest request)
    {
        var result = await  _orderService.CreateOrderProduct(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Create order successfully", HttpContext.TraceIdentifier));
    }
    
}