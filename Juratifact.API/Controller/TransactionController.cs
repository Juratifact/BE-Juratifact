using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Transactionss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[Route("api/transaction")]
[ApiController]
public class TransactionController: ControllerBase
{
    private readonly ITransactionServices _transactionServices;

    public TransactionController(ITransactionServices transactionServices)
    {
        _transactionServices = transactionServices;
    }

    [Authorize(Policy = JwtExtensions.AdminPolicy)]
    [HttpGet("")]
    public async Task<IActionResult> GetAllTransaction([FromQuery]Request.TransactionRequest request, int pageIndex = 1, int pageSize = 10)
    {
        var result = await _transactionServices.GetAllTransactions(request, pageIndex, pageSize);
        return Ok(ApiResponseFactory.SuccessResponse(result,HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpGet("seller/{sellerId}")]
    public async Task<IActionResult> GetTransactionBySellerId([FromQuery]Request.TransactionRequest request, Guid sellerId, int pageIndex = 1, int pageSize = 10)
    {
        var result = await _transactionServices.GetTransactionsBySellerId(request, sellerId, pageIndex, pageSize);
        return Ok(ApiResponseFactory.SuccessResponse(result,HttpContext.TraceIdentifier));
    }
}