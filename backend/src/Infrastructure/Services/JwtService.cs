using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Solqaryn.Infrastructure.Services;

public class JwtService : IJwtService, ITenantSessionTokenService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiraEn) GenerarTokenContexto(TenantSessionAuthority authority)
    {
        var now = DateTime.UtcNow;
        if (authority.ActorId <= 0 || authority.BaseRoleId <= 0 || authority.TenantId <= 0 ||
            authority.BranchId <= 0 || authority.TenantRoleId <= 0 ||
            authority.BaseExpiresUtc <= now)
            throw new ArgumentException("Contexto de sesión inválido.", nameof(authority));

        var secret = _configuration["Jwt:Secret"] ??
            throw new InvalidOperationException("Jwt:Secret no está configurado.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Issuer/audience JWT requeridos.");
        static string Id(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var expires = now.AddMinutes(10);
        if (authority.BaseExpiresUtc < expires) expires = authority.BaseExpiresUtc;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", Id(authority.ActorId)),
            new Claim("rolId", Id(authority.BaseRoleId)),
            new Claim("tenantId", Id(authority.TenantId)),
            new Claim("branchId", Id(authority.BranchId)),
            new Claim("tenantRoleId", Id(authority.TenantRoleId))
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(issuer, TenantSessionToken.Audience(audience),
            claims, notBefore: now, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret no está configurado.");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var expiraMinutos = int.TryParse(jwtSettings["ExpiraMinutos"], out var m) ? m : 35;
        expiraMinutos = Math.Clamp(expiraMinutos, 35, 720);
        var expiraEn = DateTime.UtcNow.AddMinutes(expiraMinutos);

        if (usuario.RolId <= 0 || usuario.RolEntidad is null ||
            usuario.RolEntidad.Id != usuario.RolId ||
            usuario.RolEntidad.Eliminado || !usuario.RolEntidad.Activo)
            throw new InvalidOperationException("El usuario no tiene un rol relacional activo válido.");

        var nombreRol = usuario.RolEntidad.Nombre;
        var esAdministrador = usuario.RolEntidad.EsAdministrador;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.NombreUsuario),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("id", usuario.Id.ToString()),
            new("nombreUsuario", usuario.NombreUsuario),
            new("nombreCompleto", usuario.NombreCompleto),
            new("rolId", usuario.RolId.ToString()),
            new("rol", nombreRol),
            new("esAdministrador", esAdministrador.ToString()),
            new(ClaimTypes.Role, nombreRol)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraEn,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
