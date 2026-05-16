using Juratifact.Service.Identity;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/auth")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _indentityService;

    public IdentityController(IIdentityService indentityService)
    {
        _indentityService = indentityService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Request.LoginRequest request)
    {
        var result = await _indentityService.Login(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Login successful", HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAccessToken()
    {
        var result = await _indentityService.RefreshAccessToken();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Refresh access token successful", HttpContext.TraceIdentifier));
    }
}
