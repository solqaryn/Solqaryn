using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class RolPermisoRepository : IRolPermisoRepository
{
    private readonly AppDbContext _context;

    public RolPermisoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RolPermiso>> GetAllAsync() =>
        await _context.RolPermisos
            .Include(p => p.Permiso)
            .AsNoTracking()
            .ToListAsync();

    public async Task<List<RolPermiso>> GetByRolIdAsync(int rolId) =>
        await _context.RolPermisos
            .Include(p => p.Permiso)
            .Where(p => p.RolId == rolId)
            .AsNoTracking()
            .ToListAsync();

    public async Task<bool> TieneMatrizDefinidaAsync(int rolId) =>
        await _context.RolPermisos.AnyAsync(p => p.RolId == rolId);

    public async Task<bool> TienePermisoPorRolIdAsync(int rolId, ModuloSistema modulo, AccionPermiso accion) =>
        await _context.RolPermisos.AnyAsync(rp =>
            rp.RolId == rolId &&
            rp.Permiso.Activo &&
            !rp.Permiso.Eliminado &&
            rp.Permiso.Modulo == modulo &&
            rp.Permiso.Accion == accion);

    public async Task ReemplazarMatrizPorRolIdAsync(int rolId, List<RolPermiso> nuevaMatriz)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var actuales = await _context.RolPermisos
            .Where(p => p.RolId == rolId)
            .ToListAsync();
        _context.RolPermisos.RemoveRange(actuales);

        var filas = nuevaMatriz
            .Where(p => p.RolId == rolId && p.PermisoId > 0)
            .GroupBy(p => new { p.RolId, p.PermisoId })
            .Select(g => g.First())
            .ToList();

        await _context.RolPermisos.AddRangeAsync(filas);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task AgregarSiFaltaAsync(List<RolPermiso> filas)
    {
        foreach (var fila in filas.Where(f => f.RolId > 0 && f.PermisoId > 0))
        {
            var existe = await _context.RolPermisos.AnyAsync(p =>
                p.RolId == fila.RolId && p.PermisoId == fila.PermisoId);
            if (!existe)
                await _context.RolPermisos.AddAsync(fila);
        }

        await _context.SaveChangesAsync();
    }
}
