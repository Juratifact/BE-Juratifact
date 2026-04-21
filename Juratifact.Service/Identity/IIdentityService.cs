namespace Juratifact.Service.Identity;

public interface IIdentityService
{
    public Task<Response.IdentityResponse> Login(string email, string password);
}