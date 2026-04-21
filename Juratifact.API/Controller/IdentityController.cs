using Juratifact.Service.Identity;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("[controller]")]
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
        var result = await _indentityService.Login(email, password);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Login successful", HttpContext.TraceIdentifier));
    }
}