using Juratifact.API.Extensions;
using Juratifact.Repository.Entity;
using Juratifact.Service.Models;
using Juratifact.Service.Promotion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
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

    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpGet("my-subscriptions")]
    public async Task<IActionResult> GetSubscribedPromotions()
    {
        var result = await _promotionService.GetSubscribedPromotions();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get subscribed promotions successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("admin/promotion-packages")]
    public async Task<IActionResult> CreatePromotionPackage(Request.PromotionRequest request)
    {
        var promotion = await _promotionService.CreatePromotion(request);
        return Ok(ApiResponseFactory.SuccessResponse(promotion, "Promotion created",HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpPost("promotion-packages/subscribe/{packageId}")]
    public async Task<IActionResult> SubscribeByPackageId(Guid packageId)
    {
        var result = await _promotionService.SubscribeByPackageId(packageId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Subscribe package successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpPost("product-promotions/apply")]
    public async Task<IActionResult> ApplyProductPromotion(Request.ProductPromotionRequest request)
    {
        var result = await _promotionService.ApplyProductPromotion(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Apply promotion successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpPatch("product-promotions/{id}/toggle")]
    public async Task<IActionResult> ChangeStatusPromotion(Guid id)
    {
        var result = await _promotionService.ChangeStatusPromotion(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Change promotion status successfully", HttpContext.TraceIdentifier));
    }
    
    
}