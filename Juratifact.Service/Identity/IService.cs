namespace Juratifact.Service.Identity;

public interface IService 
{
    public Task<Response.IdentityResponse> Login(string email, string password);
}