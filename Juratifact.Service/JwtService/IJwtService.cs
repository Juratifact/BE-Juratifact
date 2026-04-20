using System.Security.Claims;

namespace Juratifact.Service.JwtService;

public interface IJwtService
{
    public string GenerateAccessToken(IEnumerable<Claim> claims);
    
    ClaimsPrincipal ValidateToken(string token);
}