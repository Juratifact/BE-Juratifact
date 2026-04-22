using System.Text;
using Juratifact.Service.JwtService;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Juratifact.API.Extensions;

public static class JwtExtensions
{
    public const string AdminPolicy = "AdminPolicy";
    public const string SellerPolicy = "SellerPolicy";
    public const string BuyerPolicy = "BuyerPolicy";
    public const string ShipperPolicy = "ShipperPolicy";
    public const string AdminOrSellerPolicy = "AdminOrSellerPolicy";

    public static void AddJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        Jwtoptions jwtOption = new Jwtoptions();
        configuration.GetSection(nameof(Jwtoptions)).Bind(jwtOption);
        var key = Encoding.UTF8.GetBytes(jwtOption.SecretKey);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true, 
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOption.Issuer,
                    ValidAudience = jwtOption.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy =>
                policy.RequireRole("Admin"));
            // [Authorize(Policy = JwtExtensions.AdminPolicy)]
        
            options.AddPolicy(SellerPolicy, policy =>
                policy.RequireRole("Seller"));
            // [Authorize(Policy = JwtExtensions.SellerPolicy)]
        
            options.AddPolicy(BuyerPolicy, policy =>
                policy.RequireRole("Buyer"));
        
            options.AddPolicy(ShipperPolicy, policy =>
                policy.RequireRole("Shipper"));
        
            // [Authorize(Policy = JwtExtensions.SellerOrAdminPolicy)]
            options.AddPolicy(AdminOrSellerPolicy, policy =>
                policy.RequireRole("Admin", "Seller"));
        });
    }
}