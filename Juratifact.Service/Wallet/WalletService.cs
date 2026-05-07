using Juratifact.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Wallet;

public class WalletService: IWalletService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    
    
    public WalletService(AppDbContext dbContext, IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
    }
    public async Task<Response.WalletResponse> GetMyWallet()
    {
        var userId = _httpContext.HttpContext?.User.Claims
            .FirstOrDefault(x => x.Type == "UserId")?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
            throw new UnauthorizedAccessException("You are not logged in or your session has expired.");
        
        var query = _dbContext.Wallets.Where(u => u.UserId == userIdGuid && u.IsDeleted == false);

        var selected = query.Select(x => new Response.WalletResponse()
        {
            Balance = x.Balance,
            PendingBalance = x.PendingBalance,
            UpdatedAt = x.UpdatedAt,
        });

        var result = await selected.FirstOrDefaultAsync();
        return result!;
    }
}