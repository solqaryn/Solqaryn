using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Solqaryn.Application.Interfaces;

namespace Solqaryn.API.Security;

public static class TenantSessionBearer
{
    public static void Configure(JwtBearerOptions options, string issuer, string baseAudience,
        SecurityKey key)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = issuer,
            ValidateAudience = true, ValidAudience = TenantSessionToken.Audience(baseAudience),
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = key, RequireSignedTokens = true, RequireExpirationTime = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.Zero,
            IgnoreTrailingSlashWhenValidatingAudience = false
        };
    }
}
