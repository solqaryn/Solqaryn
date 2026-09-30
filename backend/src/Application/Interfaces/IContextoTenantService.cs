using Solqaryn.Domain.Security;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Resuelve el contexto tenant efectivo del usuario autenticado.
///
/// La implementación debe validar <paramref name="empresaIdSolicitada"/> contra una
/// membresía UsuarioEmpresa activa y resolver el rol desde esa membresía. El valor
/// recibido sólo expresa selección; nunca constituye autoridad por sí mismo.
/// </summary>
public interface IContextoTenantService
{
    /// <summary>
    /// Devuelve null cuando el usuario, la empresa, la membresía o el rol efectivo
    /// no pueden verificarse de forma inequívoca. No existe fallback a Usuario.RolId.
    /// </summary>
    Task<ContextoTenantActual?> ResolverActualAsync(
        int empresaIdSolicitada,
        CancellationToken cancellationToken = default);
}
