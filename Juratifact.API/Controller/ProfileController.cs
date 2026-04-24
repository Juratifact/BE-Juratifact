using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("[controller]")]
public class ProfileController: ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("GetUserById")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var profile = await _profileService.GetUserById(userId);
        return Ok(ApiResponseFactory.SuccessResponse(profile, HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("GetUserByUserName")]
    public async Task<IActionResult> GetUserByUserName(string userName)
    {
        var profiles = await _profileService.GetUserByUserName(userName);
        return Ok(ApiResponseFactory.SuccessResponse(profiles, HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("GetAllUser")]
    public async Task<IActionResult> GetAllUser(string? searchTerm, int pageSize = 10, int pageIndex = 1 )
    {
        var profiless = await _profileService.GetAllUser(searchTerm, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(profiless, HttpContext.TraceIdentifier));
    }
    
}