using Solqaryn.Domain.Entities;

namespace Solqaryn.Domain.Security;

/// <summary>
/// Contexto tenant autenticado y verificado contra una membresía UsuarioEmpresa activa.
///
/// No puede construirse desde claims o valores enviados por el cliente: la única fábrica
/// pública exige una membresía activa que coincida con el usuario y la empresa solicitados.
/// </summary>
public sealed record ContextoTenantActual
{
    private ContextoTenantActual(int usuarioId, int empresaId, int rolId)
    {
        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        RolId = rolId;
    }

    public int UsuarioId { get; }

    public int EmpresaId { get; }

    /// <summary>
    /// Rol efectivo dentro de <see cref="EmpresaId"/>. Nunca representa Usuario.RolId
    /// ni puede reutilizarse como autoridad para otra empresa.
    /// </summary>
    public int RolId { get; }

    public static ContextoTenantActual DesdeMembresia(
        UsuarioEmpresa membresia,
        int usuarioId,
        int empresaId)
    {
        ArgumentNullException.ThrowIfNull(membresia);

        var rolId = membresia.ObtenerRolEfectivo(usuarioId, empresaId);
        return new ContextoTenantActual(usuarioId, empresaId, rolId);
    }

    /// <summary>
    /// Guardia obligatoria para consumidores de entidades o consultas tenant-owned.
    /// Falla cerrado si el identificador no pertenece al contexto verificado.
    /// </summary>
    public void ExigirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(empresaId),
                "El identificador de Empresa debe ser positivo.");
        }

        if (EmpresaId != empresaId)
        {
            throw new InvalidOperationException(
                "El recurso solicitado no pertenece al contexto tenant verificado.");
        }
    }
}
