using Juratifact.API.Extensions;
using Juratifact.Repository.Enum;
using Juratifact.Service.IdentityDocumentService;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IdentifyDocumentController: ControllerBase
{
    private readonly IIdentityDocumentService _identityDocumentService;
    
    public IdentifyDocumentController(IIdentityDocumentService identityDocumentService)
    {
        _identityDocumentService = identityDocumentService;
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("Submit")]
    public async Task<IActionResult> SubmitIdentityDocument(Request.UploadIdentityDocumentRequest request)
    {
        await _identityDocumentService.SubmitIdentityDocumentAsync(request);
        return Ok(ApiResponseFactory.SuccessResponse(null, "Summit successful", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("Re-Submit")]
    public async Task<IActionResult> ReSubmitIdentityDocument(Request.ReUploadIdentityDocumentRequest request)
    {
        await _identityDocumentService.ReSubmitIdentityDocumentAsync(request);
        return Ok(ApiResponseFactory.SuccessResponse(null, "Summit successful", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("GetAll/StatusPending")]
    public async Task<IActionResult> GetAll(IdentityStatus? status, int pageIndex = 1, int pageSize = 10)
    {
        
        var documents = await _identityDocumentService.GetAllAsync(status, pageIndex, pageSize);
        return Ok(ApiResponseFactory.SuccessResponse(documents, "Get all identity document successfully", HttpContext.TraceIdentifier));
    }
    
    
    [HttpGet("GetById")]
    public async Task<IActionResult> GetById(Guid documentId)
    {
        var document = await _identityDocumentService.GetByIdAsync(documentId);
        return Ok(ApiResponseFactory.SuccessResponse(document, "Get identity document successfully", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("GetMyDocument")]
    public async Task<IActionResult> GetMyDocument()
    {
        var document = await _identityDocumentService.GetMyDocumentAsync();
        return Ok(ApiResponseFactory.SuccessResponse(document, "Get my identity document successfully", HttpContext.TraceIdentifier));
    }

    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPut("Approve")]
    public async Task<IActionResult> Approve(Guid documentId)
    {
        await _identityDocumentService.ApproveAsync(documentId);
        return Ok(ApiResponseFactory.SuccessResponse(null, "Approve identity document successfully", HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPut("Reject")]
    public async Task<IActionResult> Reject(Guid documentId, string reason)
    {
        await _identityDocumentService.RejectAsync(documentId, reason);
        return Ok(ApiResponseFactory.SuccessResponse(null, "Reject identity document successfully", HttpContext.TraceIdentifier));
    }
}