using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class EvaluacionProveedorRepository : IEvaluacionProveedorRepository
{
    private readonly AppDbContext _db;

    public EvaluacionProveedorRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<(IReadOnlyList<EvaluacionProveedor> Items, int Total)> GetPagedAsync(EvaluacionProveedorFiltroDto filtro)
    {
        IQueryable<EvaluacionProveedor> query = _db.Set<EvaluacionProveedor>().AsNoTracking();
        if (filtro.ProveedorId.HasValue) query = query.Where(x => x.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.OrdenCompraId.HasValue) query = query.Where(x => x.OrdenCompraId == filtro.OrdenCompraId.Value);
        if (filtro.RecepcionCompraId.HasValue) query = query.Where(x => x.RecepcionCompraId == filtro.RecepcionCompraId.Value);
        if (filtro.DesdeUtc.HasValue) query = query.Where(x => x.FechaRecepcionUtc >= filtro.DesdeUtc.Value);
        if (filtro.HastaUtc.HasValue) query = query.Where(x => x.FechaRecepcionUtc <= filtro.HastaUtc.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.FechaRecepcionUtc)
            .ThenByDescending(x => x.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();
        return (items, total);
    }

    public Task<EvaluacionProveedor?> GetByIdAsync(int id, bool tracking = false)
    {
        IQueryable<EvaluacionProveedor> query = _db.Set<EvaluacionProveedor>();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<EvaluacionProveedor?> GetByRecepcionCompraIdAsync(int recepcionCompraId, bool tracking = false)
    {
        IQueryable<EvaluacionProveedor> query = _db.Set<EvaluacionProveedor>();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.RecepcionCompraId == recepcionCompraId);
    }

    public Task AddAsync(EvaluacionProveedor evaluacion) => _db.Set<EvaluacionProveedor>().AddAsync(evaluacion).AsTask();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
