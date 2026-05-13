using Juratifact.Service.Models;
using Juratifact.Service.VietMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Authorize]
[ApiController]
[Route("api/vietmap")]
public class VietMapController : ControllerBase
{
    private readonly IVietMapService _vietMapService;

    public VietMapController(IVietMapService vietMapService)
    {
        _vietMapService = vietMapService;
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete(
        [FromQuery] string text,
        [FromQuery] string? focus,
        [FromQuery] int displayType = 5)
    {
        var result = await _vietMapService.AutocompleteAsync(text, focus, displayType, HttpContext.RequestAborted);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get VietMap autocomplete successfully", HttpContext.TraceIdentifier));
    }

    [HttpGet("places/{refId}")]
    public async Task<IActionResult> GetPlaceDetail([FromRoute] string refId)
    {
        var result = await _vietMapService.GetPlaceDetailAsync(refId, HttpContext.RequestAborted);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get VietMap place detail successfully", HttpContext.TraceIdentifier));
    }
}
