using System.Security.Claims;

namespace Juratifact.Service.JwtService;

public interface IService
{
    public string GenerateAccessToken(IEnumerable<Claim> claims);
    
    ClaimsPrincipal ValidateToken(string token); 

}