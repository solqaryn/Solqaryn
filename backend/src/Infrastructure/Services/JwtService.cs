using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Solqaryn.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
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

        if (usuario.RolId <= 0 || usuario.RolEntidad is null || usuario.RolEntidad.Eliminado || !usuario.RolEntidad.Activo)
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
