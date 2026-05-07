namespace Juratifact.Service.Wallet;

public interface IWalletService
{
    public Task<Response.WalletResponse> GetMyWallet();
}