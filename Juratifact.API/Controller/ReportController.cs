using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ReportController:ControllerBase
{
    private readonly IReportService _reportService;
    
    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("CreateReport")]
    public async Task<IActionResult> CreateReport(Request.ReportRequest request)
    {
        var report = await _reportService.CreateReport(request);
        return Ok(ApiResponseFactory.SuccessResponse(report,"Report created", HttpContext.TraceIdentifier ));
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("GetReport")]
    public async Task<IActionResult> GetReport(string? searchTerm, int pageSize = 10, int pageIndex = 1)
    {
        var reports = await _reportService.GetReport(searchTerm, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(reports, HttpContext.TraceIdentifier));
    }
    
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPut("AproveReport/BannedProduct")]
    public async Task<IActionResult> ApproveReport(Guid reportId)
    {
        var result = await _reportService.ApproveReport(reportId);
        return Ok(ApiResponseFactory.SuccessResponse(result, HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpPut("RejectReport")]
    public async Task<IActionResult> RejectReport(Guid reportId)
    {
        var result = await _reportService.RejectReport(reportId);
        return Ok(ApiResponseFactory.SuccessResponse(result, HttpContext.TraceIdentifier));
    }
}