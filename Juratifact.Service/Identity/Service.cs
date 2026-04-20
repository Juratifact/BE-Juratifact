using System.Security.Claims;
using Juratifact.Repository;
using Juratifact.Service.JwtService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Juratifact.Service.Identity;

public class Service : IService
{
    private readonly JwtService.IService _jwtService;
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _jwtOption = new();
    
    public Service(IConfiguration configuration, JwtService.IService jwtService, AppDbContext dbContext)
    {
        _jwtService = jwtService;
        _dbContext = dbContext;
        configuration.GetSection(nameof(JwtOptions)).Bind(_jwtOption);
    }
    
    public async Task<Response.IdentityResponse> Login(string email, string password)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null)
        {
            throw new Exception("User not found");
        }
        
        if(user.HashedPassword != password)
        {
            throw new Exception("Invalid password");
        }
        
        var roles = user.UserRoles
            .Where(userRole => userRole.Role! != null)
            .Select(userRole => userRole.Role!.Name)
            .ToList();
        
        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Expired, 
                DateTimeOffset.UtcNow.AddMinutes(_jwtOption.ExpireMinutes).ToString()),
        };
        
        foreach (var role in roles)
        {
            claims.Add(new Claim("Role", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        var token = _jwtService.GenerateAccessToken(claims);
        
        var result = new Response.IdentityResponse()
        {
            AccessToken = token
        };

        return result;
        
    }
}