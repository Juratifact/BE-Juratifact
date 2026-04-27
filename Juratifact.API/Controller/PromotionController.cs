using Juratifact.API.Extensions;
using Juratifact.Repository.Entity;
using Juratifact.Service.Models;
using Juratifact.Service.Promotion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("[controller]")]
public class PromotionController:ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }
    
    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpGet("promotion-packages/available")]
    public async Task<IActionResult> GetPromotionPackages()
    {
        var result = await _promotionService.GetPromotionPackages();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get promotion packages successfully", HttpContext.TraceIdentifier));
    }

    [HttpPost("api/admin/promotion-packages")]
    public Task<IActionResult> CreatePromotionPackage()
    {
        return null;
    }
}