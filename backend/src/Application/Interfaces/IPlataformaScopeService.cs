namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Authority verified from UsuarioRolesPlataforma + Roles + Usuarios.
/// No EmpresaId, tenant membership or JWT role claim grants this authority.
/// </summary>
public sealed record UsuarioPlataformaScopeActual(
    int UsuarioId, int RolId, string NombreUsuario, string RolNombre);

public interface IPlataformaScopeService
{
    Task<UsuarioPlataformaScopeActual?> ObtenerActualAsync(
        CancellationToken cancellationToken = default);
}
