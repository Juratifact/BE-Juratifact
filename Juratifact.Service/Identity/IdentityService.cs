using System.Security.Claims;
using Juratifact.Repository;
using Juratifact.Service.JwtService;
using Juratifact.Service.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Juratifact.Service.Identity;

public class IdentityService: IIdentityService
{
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly Jwtoptions _jwtOption = new();

    public IdentityService(IJwtService jwtService, AppDbContext dbContext, IHttpContextAccessor httpContext, IConfiguration configuration)
    {
        _jwtService = jwtService;
        _dbContext = dbContext;
        _httpContext = httpContext;
        configuration.GetSection(nameof(Jwtoptions)).Bind(_jwtOption);
    }

    public async Task<Response.IdentityResponse> Login(Request.LoginRequest request)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // // Kiểm tra mật khẩu bằng Argon2
        bool isPasswordValid = Argon2Hasher.VerifyHash(request.Password, user.HashedPassword);
        
        if (!isPasswordValid) //user.HashedPassword != password
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Expired, 
                DateTimeOffset.UtcNow.AddMinutes(_jwtOption.ExpireMinutes).ToString()),
        };

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(GetRolePriority)
            .ThenBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var roleName in roles)
        {
            claims.Add(new Claim("Role", roleName));
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var token = _jwtService.GenerateAccessToken(claims);
        
        var result = new Response.IdentityResponse()
        {
            Access_token = token,
            UserId = user.Id,
            IsVerify = user.IsVerify,
            Roles = roles
        };

        return result;
    }

    public async Task<Response.IdentityResponse> RefreshAccessToken()
    {
        var userId = _httpContext.HttpContext?.User?.FindFirst("UserId")?.Value;

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userIdGuid && u.IsDeleted == false);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(GetRolePriority)
            .ThenBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Expired, DateTimeOffset.UtcNow.AddMinutes(_jwtOption.ExpireMinutes).ToString()),
        };

        foreach (var roleName in roles)
        {
            claims.Add(new Claim("Role", roleName));
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var token = _jwtService.GenerateAccessToken(claims);

        return new Response.IdentityResponse
        {
            Access_token = token,
            UserId = user.Id,
            IsVerify = user.IsVerify,
            Roles = roles
        };
    }

    private static int GetRolePriority(string roleName)
    {
        if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (roleName.Equals("Seller", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (roleName.Equals("Shipper", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (roleName.Equals("Buyer", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }
}
