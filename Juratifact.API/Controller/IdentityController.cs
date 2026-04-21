using Juratifact.Service.Identity;
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

    [HttpGet("")]
    public async Task<IActionResult> Login(string  email, string password)
    {
        var result = await _indentityService.Login(email, password);
        return Ok(result);
    }
}