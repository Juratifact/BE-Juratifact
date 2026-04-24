using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly  IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> CreateUser([FromForm] Request.CreateUserRequest request)
    {
        var result =  await _userService.CreateUser(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "User created", HttpContext.TraceIdentifier));
    }
    
    [HttpPut("Profile/{id}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromForm] Request.UpdateUserRequest request)
    {
        var result =  await _userService.UpdateUser(id, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "User updated", HttpContext.TraceIdentifier));
    }
    
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDeleteUser(Guid id)
    {
        var result = await _userService.SoftDeleteUser(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "User removed", HttpContext.TraceIdentifier));
    }
}