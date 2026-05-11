namespace Juratifact.Service.Identity;

public interface IIdentityService
{
    public Task<Response.IdentityResponse> Login(Request.LoginRequest request);
}