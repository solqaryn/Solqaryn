using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.Security;

/// <summary>
/// Instantánea leída de una fuente confiable en el servidor. No aceptar este
/// objeto desde DTO, claim o header: el proveedor debe recargar membresía y rol.
/// </summary>
public sealed record IdentidadUniversalSnapshot(
    Usuario Usuario,
    Empresa Empresa,
    UsuarioEmpresa Membresia,
    Rol Rol,
    Sucursal? Sucursal);

public interface IIdentidadUniversalSnapshotSource
{
    Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
        int usuarioId,
        int empresaId,
        int? sucursalId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Revalida el contexto autenticado contra datos empresariales actuales.
/// Esta pieza es Application-only; no registra endpoints, DI ni rutas de bypass.
/// </summary>
public sealed class IdentidadUniversalApplicationService
{
    private readonly IIdentidadUniversalSnapshotSource _source;

    public IdentidadUniversalApplicationService(IIdentidadUniversalSnapshotSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public async Task<ContextoTenantActual> ResolverActualAsync(
        ContextoTenantActual autenticado,
        int empresaId,
        int? sucursalId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(autenticado);
        // Antes de cualquier I/O: un actor tenant A nunca consulta datos de B.
        autenticado.ExigirEmpresa(empresaId);

        if (sucursalId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(sucursalId));

        var actual = await _source.ObtenerActualAsync(
            autenticado.UsuarioId, empresaId, sucursalId, cancellationToken)
            ?? throw new InvalidOperationException(
                "No existe una membresia vigente en el alcance solicitado.");

        // Los IDs de la instantánea son datos de persistencia, no permisos.
        var verificado = IdentidadUniversalReglas.CrearContextoVerificado(
            actual.Usuario, actual.Empresa, actual.Membresia, actual.Rol);

        if (verificado.UsuarioId != autenticado.UsuarioId ||
            verificado.EmpresaId != autenticado.EmpresaId ||
            verificado.RolId != autenticado.RolId)
            throw new InvalidOperationException(
                "El contexto ya no coincide con la membresia autorizada.");

        if (sucursalId.HasValue)
        {
            if (actual.Sucursal is null || actual.Sucursal.Id != sucursalId.Value)
                throw new InvalidOperationException(
                    "La sucursal solicitada no pertenece a la instantanea confiable.");

            IdentidadUniversalReglas.ExigirSucursal(verificado, actual.Sucursal);
        }
        else if (actual.Sucursal is not null)
        {
            // Una consulta sin sucursal no puede adquirirla implícitamente.
            throw new InvalidOperationException(
                "La fuente devolvio una sucursal que no fue solicitada.");
        }

        return verificado;
    }
}
