using Juratifact.Service.Identity;
using Microsoft.AspNetCore.Mvc;
using TetPee.Service.Models;

namespace Juratifact.API.Controller;

[ApiController]
[Route("[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IService _identityService;

    public IdentityController(IService identityService)
    {
        _identityService = identityService;
    }
    
    [HttpGet("login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        var result = await _identityService.Login(email, password);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Login successful", HttpContext.TraceIdentifier));
    }
}