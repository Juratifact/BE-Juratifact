using Juratifact.API.Extensions;
using Juratifact.Service.Dispute;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class DisputeController : ControllerBase
{
    private readonly IDisputeService _disputeService;

    public DisputeController(IDisputeService disputeService)
    {
        _disputeService = disputeService;
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("{orderId}/create")]
    public async Task<IActionResult> CreateDispute([FromRoute] Guid orderId,
        [FromBody] Request.CreateDisputeRequest request)
    {
        var result = await  _disputeService.CreateDispute(orderId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Created Dispute successfully", HttpContext.TraceIdentifier));
    }
    
    // /api/dispute/my-disputes
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("my-disputes")]
    public async Task<IActionResult> CreateDisputes([FromBody] Request.CreateDisputeRequest request)
    {
        return null;
    }
    
    

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPost("admin/{disputeId}/resolve")]
    public async Task<IActionResult> ResolveDispute([FromRoute] Guid disputeId,
        [FromBody] Request.ResolveDisputeRequest request)
    {
        var result = await _disputeService.ResolveDispute(disputeId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Resolved Dispute successfully", HttpContext.TraceIdentifier));
    }
    
}