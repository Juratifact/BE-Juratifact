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

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("api/admin/promotion-packages")]
    public async Task<IActionResult> CreatePromotionPackage(Request.PromotionRequest request)
    {
        var promotion = await _promotionService.CreatePromotion(request);
        return Ok(ApiResponseFactory.SuccessResponse(promotion, "Promotion created",HttpContext.TraceIdentifier));
    }
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("api/admin/reports/subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        var promotions = await _promotionService.GetSubscriptions();
        return Ok(ApiResponseFactory.SuccessResponse(promotions,HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("api/admin/reports/subscriptions")]
    public async Task<IActionResult> DeletePromotion(Guid id)
    {
        var promotion1 = await _promotionService.DeletePromotion(id);
        return Ok(ApiResponseFactory.SuccessResponse(promotion1,"Delete successfully",HttpContext.TraceIdentifier));
    }
}