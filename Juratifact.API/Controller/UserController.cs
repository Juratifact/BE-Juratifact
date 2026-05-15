using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid id)
    {
        var user = await _userService.GetUserProfile(id);
        return Ok(ApiResponseFactory.SuccessResponse(user, "Get user profile successfully", HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllUser(string? searchTerm, int pageIndex = 1, int pageSize = 10)
    {
        var user = await _userService.GetAllUser(searchTerm, pageIndex, pageSize);
        return Ok(ApiResponseFactory.SuccessResponse(user, "Get user profile successfully", HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("by-username/{userName}")]
    public async Task<IActionResult> GetUserByName(string userName)
    {
        var user = await _userService.GetUserByName(userName);
        return Ok(ApiResponseFactory.SuccessResponse(user, "Get user profile successfully", HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromForm] Request.CreateUserRequest request)
    {
        var result = await _userService.CreateUser(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "User created", HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromForm] Request.UpdateUserRequest request)
    {
        var result = await _userService.UpdateUser(id, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "User updated", HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDeleteUser(Guid id)
    {
        var result = await _userService.SoftDeleteUser(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "User removed", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("shippers")]
    public async Task<IActionResult> AdminRegisterAdmin([FromForm] Request.CreateShipperRequest request)
    {
        var result = await _userService.CreatShipper(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Shipper created by Admin", HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("roles")]
    public async Task<IActionResult> GetMyRole()
    {
        var result = await _userService.GetMyRole();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get My Role", HttpContext.TraceIdentifier));
    }
        
}
