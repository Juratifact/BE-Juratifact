using Juratifact.Repository.Entity;
using Juratifact.Service.Promotion;
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

    [HttpPost("api/admin/promotion-packages")]
    public Task<IActionResult> CreatePromotionPackage()
    {
        return null;
    }
}