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

    [Authorize]
    [HttpGet("MyProfile")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var user = await _userService.GetUserProfile(userId);
        return Ok(ApiResponseFactory.SuccessResponse(user, "Get user profile successfully", HttpContext.TraceIdentifier));

    }
    
    [Authorize]
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllUser(string? searchTerm, int pageIndex = 1, int pageSize = 10)
    {
        var user = await _userService.GetAllUser(searchTerm, pageIndex, pageSize);
        return Ok(ApiResponseFactory.SuccessResponse(user, "Get user profile successfully", HttpContext.TraceIdentifier));

    }
    
   [Authorize]
    [HttpGet("GetUserByName")]
    public async Task<IActionResult> GetUserByName(string userName)
    {
        var user = await _userService.GetUserByName(userName);
        return Ok(ApiResponseFactory.SuccessResponse(user, "Get user profile successfully", HttpContext.TraceIdentifier));

    }
    
}