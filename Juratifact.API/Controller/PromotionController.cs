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

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("api/admin/promotion-packages")]
    public async Task<IActionResult> CreatePromotionPackage(Request.PromotionRequest request)
    {
        var promotion = await _promotionService.CreatePromotion(request);
        return Ok(ApiResponseFactory.SuccessResponse(promotion, "Promotion created",HttpContext.TraceIdentifier));
    }
}