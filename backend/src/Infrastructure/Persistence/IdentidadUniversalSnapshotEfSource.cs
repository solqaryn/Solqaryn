using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// C01-01.M03.04: adaptador EF de solo lectura para una instantanea de identidad
/// obtenida del servidor. La capa Application debe verificar el tenant autenticado
/// antes de llamar al adaptador; esta fuente NO es un endpoint ni concede permisos.
/// </summary>
public sealed class IdentidadUniversalSnapshotEfSource : IIdentidadUniversalSnapshotSource
{
    private readonly AppDbContext _db;

    public IdentidadUniversalSnapshotEfSource(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
        int usuarioId, int empresaId, int? sucursalId,
        CancellationToken cancellationToken)
    {
        // Fail-closed, incluso si un consumidor omite validaciones previas.
        if (usuarioId <= 0 || empresaId <= 0 || sucursalId is <= 0)
            return null;

        // No leer otras empresas ni utilizar el RolId legacy del Usuario.
        // SingleOrDefault exige membresia unica y no escoge filas arbitrarias.
        var membresia = await _db.Set<UsuarioEmpresa>().AsNoTracking()
            .SingleOrDefaultAsync(m =>
                m.UsuarioId == usuarioId &&
                m.EmpresaId == empresaId && m.Activa, cancellationToken);

        if (membresia is null)
            return null;

        var usuario = await _db.Set<Usuario>().AsNoTracking()
            .SingleOrDefaultAsync(u =>
                u.Id == usuarioId && u.Activo && !u.Bloqueado && !u.Eliminado,
                cancellationToken);

        var empresa = await _db.Set<Empresa>().AsNoTracking()
            .SingleOrDefaultAsync(e =>
                e.Id == empresaId && e.Activa, cancellationToken);

        if (usuario is null || empresa is null)
            return null;

        // Cargar la concesion relacional desde el servidor; un rol legacy
        // o un permiso de otra empresa/plataforma nunca se hereda por defecto.
        var rol = await _db.Set<Rol>().AsNoTracking()
            .Include(r => r.Permisos)
            .ThenInclude(p => p.Permiso)
            .SingleOrDefaultAsync(r =>
                r.Id == membresia.RolId && r.Activo && !r.Eliminado,
                cancellationToken);

        if (rol is null || !rol.EsAsignableAEmpresa(empresaId))
            return null;

        Sucursal? sucursal = null;
        if (sucursalId.HasValue)
        {
            sucursal = await _db.Set<Sucursal>().AsNoTracking()
                .SingleOrDefaultAsync(s =>
                    s.Id == sucursalId.Value &&
                    s.EmpresaId == empresaId &&
                    s.Activa && !s.Eliminado, cancellationToken);
            if (sucursal is null)
                return null;
        }

        return new IdentidadUniversalSnapshot(
            usuario, empresa, membresia, rol, sucursal);
    }
}
