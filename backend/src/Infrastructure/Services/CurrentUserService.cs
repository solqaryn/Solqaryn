using System.Security.Claims;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool EstaAutenticado => User?.Identity?.IsAuthenticated ?? false;

    public int? UsuarioId
    {
        get
        {
            var value = User?.FindFirstValue("id");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? NombreUsuario => User?.FindFirstValue("nombreUsuario");

    public string? NombreCompleto => User?.FindFirstValue("nombreCompleto");

    public int? RolId
    {
        get
        {
            var value = User?.FindFirstValue("rolId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public bool EsAdministrador
    {
        get
        {
            // Informativo únicamente. IPermisoService nunca concede por este claim;
            // resuelve el rol vigente y sus grants desde MySQL.
            var value = User?.FindFirstValue("esAdministrador");
            return bool.TryParse(value, out var esAdmin) && esAdmin;
        }
    }
}
