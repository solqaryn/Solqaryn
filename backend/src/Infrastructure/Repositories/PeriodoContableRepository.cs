using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class PeriodoContableRepository : IPeriodoContableRepository
{
    private readonly AppDbContext _context;

    public PeriodoContableRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PeriodoContable?> GetByIdAsync(int id, bool tracking = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PeriodosContables.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PeriodoContable?> GetByDateAsync(DateTime date, bool tracking = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PeriodosContables.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.FechaInicio <= date && p.FechaFin >= date, cancellationToken);
    }

    public Task<bool> IsValidDateAsync(DateTime date, CancellationToken cancellationToken = default) =>
        _context.PeriodosContables.AnyAsync(
            p => p.Estado == EstadoPeriodoContable.Abierto && p.FechaInicio <= date && p.FechaFin >= date,
            cancellationToken);

    public Task<bool> HasOverlappingPeriodAsync(DateTime fechaInicio, DateTime fechaFin, int? excludeId = null, CancellationToken cancellationToken = default) =>
        _context.PeriodosContables.AnyAsync(
            p => (excludeId == null || p.Id != excludeId) && p.FechaInicio <= fechaFin && p.FechaFin >= fechaInicio,
            cancellationToken);

    public async Task<PagedResult<PeriodoContable>> GetPagedAsync(PeriodoContableQueryDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.PeriodosContables.AsNoTracking();

        if (filter.FechaDesde.HasValue)
            query = query.Where(p => p.FechaInicio >= filter.FechaDesde.Value);

        if (filter.FechaHasta.HasValue)
            query = query.Where(p => p.FechaFin <= filter.FechaHasta.Value);

        if (filter.Estado.HasValue)
            query = query.Where(p => p.Estado == filter.Estado.Value);

        query = query.OrderByDescending(p => p.FechaInicio);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PeriodoContable>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public Task AddAsync(PeriodoContable periodo, CancellationToken cancellationToken = default) =>
        _context.PeriodosContables.AddAsync(periodo, cancellationToken).AsTask();

    public void Update(PeriodoContable periodo) => _context.PeriodosContables.Update(periodo);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
