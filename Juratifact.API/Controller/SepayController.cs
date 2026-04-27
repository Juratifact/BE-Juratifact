using Juratifact.Service.Sepay;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SepayController: ControllerBase
{
    private readonly ISepayService _sepayService;

    public SepayController(ISepayService sepayService)
    {
        _sepayService = sepayService;
    }

    
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] Request.SepayWebhookDto payload)
    {
        var result = await _sepayService.ProcessSePayWebhook(payload);
        if (result)
            return Ok(ApiResponseFactory.SuccessResponse(null, "Webhook processed", HttpContext.TraceIdentifier));

        return BadRequest(ApiResponseFactory.ErrorResponse("Failed to match transaction for webhook", null, HttpContext.TraceIdentifier));
    }

    
    [AllowAnonymous]
    [HttpGet("qrcode")]
    public async Task<IActionResult> GenerateQr([FromQuery] decimal amount, [FromQuery] string referenceCode)
    {
        var url = await _sepayService.GenerateQrCode(amount, referenceCode);
        return Ok(ApiResponseFactory.SuccessResponse(url, "QR code generated", HttpContext.TraceIdentifier));
    }
}