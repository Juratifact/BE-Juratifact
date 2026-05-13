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
    [HttpGet("api/transactions")]
    public async Task<IActionResult> GetAllTransaction(Request.TransactionRequest request, int pageSize = 10, int pageIndex = 1)
    {
        var result = await _transactionServices.GetAllTransactions(request, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result,HttpContext.TraceIdentifier));
    }
    
    [Authorize(Policy = JwtExtensions.SellerPolicy)]
    [HttpGet("api/transactions/seller/{sellerId}")]
    public async Task<IActionResult> GetTransactionBySellerId(Request.TransactionRequest request, Guid sellerId, int pageSize = 10, int pageIndex = 1)
    {
        var result = await _transactionServices.GetTransactionsBySellerId(request, sellerId, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(result,HttpContext.TraceIdentifier));
    }
}