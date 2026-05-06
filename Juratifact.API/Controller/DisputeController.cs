using Juratifact.API.Extensions;
using Juratifact.Repository.Enum;
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
    [HttpPost("create/{orderId}")]
    public async Task<IActionResult> CreateDispute([FromRoute] Guid orderId,
        [FromBody] Request.CreateDisputeRequest request)
    {
        var result = await  _disputeService.CreateDispute(orderId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Created Dispute successfully", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("my-disputes")]
    public async Task<IActionResult> CreateDisputes(int pageSize = 10, int pageIndex = 1)
    {
        var result = await _disputeService.GetMyDispute(pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get my disputes successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("{disputeId}/cancel")]
    public async Task<IActionResult> CancelDispute(Guid disputeId)
    {
        var result = await _disputeService.CancelDispute(disputeId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Cancelled Dispute successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("admin/disputes")]
    public async Task<IActionResult> GetAllDisputes(DisputeStatus? status, int pageSize = 10, int pageIndex = 1)
    {
        var result = await _disputeService.GetDisputes(status, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get Disputes successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPatch("admin/{disputeId}/assign")]
    public async Task<IActionResult> AssignDispute(Guid disputeId, [FromBody] Request.AssignDisputeRequest request)
    {
        // thằng admin nào nhận cái ca này
        var result = await _disputeService.AssignDispute(disputeId, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Assigned Dispute successfully", HttpContext.TraceIdentifier));
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