using Juratifact.Repository;
using Juratifact.Service.Models;
using Juratifact.Service.User;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly  IUserService _userService;
    
    public UserController(AppDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> CreateUser([FromForm] UserRequest.CreateUserRequest request)
    {
        var result =  await _userService.CreateUser(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "User created", HttpContext.TraceIdentifier));
    }
}