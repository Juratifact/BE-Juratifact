using System.Security.Claims;
using Juratifact.Repository;
using Juratifact.Service.JwtService;
using Juratifact.Service.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Juratifact.Service.Identity;

public class IdentityService: IIdentityService
{
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _dbContext;
    private readonly Jwtoptions _jwtOption = new();

    public IdentityService(IJwtService jwtService, AppDbContext dbContext, IConfiguration configuration)
    {
        _jwtService = jwtService;
        _dbContext = dbContext;
        configuration.GetSection(nameof(Jwtoptions)).Bind(_jwtOption);
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
        
        // // Kiểm tra mật khẩu bằng Argon2
        bool isPasswordValid = Argon2Hasher.VerifyHash(password, user.HashedPassword);
        
        if (!isPasswordValid) //user.HashedPassword != password
        {
            throw new Exception("Invalid password");
        }
        
        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Expired, 
                DateTimeOffset.UtcNow.AddMinutes(_jwtOption.ExpireMinutes).ToString()),
        };

        if (user.UserRoles.Any())
        {
            foreach (var userRole in user.UserRoles)
            {
                var roleName = userRole.Role.Name;
                claims.Add(new Claim("Role", roleName));
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }
        }

        
        var token = _jwtService.GenerateAccessToken(claims);
        
        var result = new Response.IdentityResponse()
        {
            Access_token = token
        };

        return result;
    }
}