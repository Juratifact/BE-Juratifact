using Juratifact.Repository;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Wallet;

public class WalletService: IWalletService
{
    private readonly AppDbContext _dbContext;
    public WalletService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Response.WalletResponse> GetMyWallet(Guid userId)
    {
        var query = _dbContext.Wallets.Where(u => u.UserId == userId);

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