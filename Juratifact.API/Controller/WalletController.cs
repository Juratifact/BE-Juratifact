using Juratifact.Service.Models;
using Juratifact.Service.Wallet;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class WalletController:ControllerBase
{
    private readonly IWalletService _walletService;
    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet("api/wallet/get-wallet")]
    public async Task<IActionResult> GetWallet(Guid userId)
    {
        var result = await _walletService.GetMyWallet(userId);
        return Ok(ApiResponseFactory.SuccessResponse(result,"successfully",HttpContext.TraceIdentifier));
    }
    
}