namespace Solqaryn.Application.Interfaces;

public interface ICurrentUserService
{
    int? UsuarioId { get; }
    string? NombreUsuario { get; }
    string? NombreCompleto { get; }

    /// Id del rol relacional emitido en el JWT. La autorización efectiva vuelve a
    /// resolver usuario/rol desde base de datos mediante IUsuarioScopeService.
    int? RolId { get; }

    /// Claim informativo emitido desde Rol.EsAdministrador. No concede permisos
    /// por sí mismo; los grants se verifican siempre en RolPermiso.
    bool EsAdministrador { get; }

    bool EstaAutenticado { get; }
}
