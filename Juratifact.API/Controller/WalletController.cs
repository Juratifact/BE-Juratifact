using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Wallet;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpGet("my-wallet")]
    public async Task<IActionResult> GetWallet()
    {
        var result = await _walletService.GetMyWallet();
        return Ok(ApiResponseFactory.SuccessResponse(result,"Get wallet successfully",HttpContext.TraceIdentifier));
    }
    
}