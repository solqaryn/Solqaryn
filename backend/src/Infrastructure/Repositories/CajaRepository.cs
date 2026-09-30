using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Cajas;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// EF/MySQL repository for the Caja aggregate. Locking reads are fail-closed and require
/// an active transaction, matching the repository pattern used by other mutable ERP aggregates.
/// </summary>
public sealed class CajaRepository : ICajaRepository
{
    private readonly AppDbContext _context;

    public CajaRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private DbSet<Caja> Cajas => _context.Set<Caja>();
    private DbSet<CajaSesion> Sesiones => _context.Set<CajaSesion>();

    public async Task<Caja?> GetCajaByIdAsync(int id, bool tracking = false)
    {
        var query = tracking ? Cajas.AsTracking() : Cajas.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Caja?> GetCajaByIdForUpdateAsync(int id)
    {
        RequireTransaction();
        return await Cajas
            .FromSqlInterpolated($"SELECT c.* FROM Cajas c WHERE c.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<CajaSesion?> GetSesionByIdAsync(int id, bool tracking = false)
    {
        var query = tracking ? Sesiones.AsTracking() : Sesiones.AsNoTracking();
        return await query
            .Include(x => x.Movimientos)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CajaSesion?> GetSesionByIdForUpdateAsync(int id)
    {
        RequireTransaction();
        var sesion = await Sesiones
            .FromSqlInterpolated($"SELECT s.* FROM CajaSesiones s WHERE s.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (sesion is not null)
            await _context.Entry(sesion).Collection(x => x.Movimientos).LoadAsync();

        return sesion;
    }

    public async Task<CajaSesion?> GetSesionActivaByCajaIdAsync(int cajaId, bool tracking = false)
    {
        var query = tracking ? Cajas.AsTracking() : Cajas.AsNoTracking();
        var sesionId = await query
            .Where(x => x.Id == cajaId)
            .Select(x => x.SesionActivaId)
            .FirstOrDefaultAsync();

        return sesionId.HasValue
            ? await GetSesionByIdAsync(sesionId.Value, tracking)
            : null;
    }

    public Task AddCajaAsync(Caja caja) => Cajas.AddAsync(caja).AsTask();
    public Task AddSesionAsync(CajaSesion sesion) => Sesiones.AddAsync(sesion).AsTask();
    public void UpdateCaja(Caja caja) => Cajas.Update(caja);
    public void UpdateSesion(CajaSesion sesion) => Sesiones.Update(sesion);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

    private void RequireTransaction()
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La operación de Caja con bloqueo requiere una transacción activa.");
    }
}
