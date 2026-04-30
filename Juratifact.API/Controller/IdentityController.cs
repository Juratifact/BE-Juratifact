using Juratifact.Service.Identity;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _indentityService;

    public IdentityController(IIdentityService indentityService)
    {
        _indentityService = indentityService;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login(string  email, string password)
    {
        // throw new Exception("Cảnh báo giả: Test thử xem Discord có réo tên không nè!");
        var result = await _indentityService.Login(email, password);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Login successful", HttpContext.TraceIdentifier));
    }
}