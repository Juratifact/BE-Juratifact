using Juratifact.Service.Identity;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _indentityService;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(IIdentityService indentityService, ILogger<IdentityController> logger)
    {
        _indentityService = indentityService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Request.LoginRequest request)
    {
        var result = await _indentityService.Login(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Login successful", HttpContext.TraceIdentifier));
    }
}