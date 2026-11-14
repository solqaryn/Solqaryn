using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Infrastructure.Repositories;

public class ConciliacionBancariaRepository : IConciliacionBancariaRepository
{
    private readonly AppDbContext _context;

    public ConciliacionBancariaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConciliacionBancaria?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConciliacionBancaria>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ConciliacionBancaria?> GetByPeriodoAsync(int cuentaBancariaId, int mes, int anio, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConciliacionBancaria>()
            .FirstOrDefaultAsync(c => c.CuentaBancariaId == cuentaBancariaId && c.FechaInicio.Month == mes && c.FechaInicio.Year == anio, cancellationToken);
    }

    public async Task<ConciliacionBancaria?> GetActivaByCuentaAsync(int cuentaBancariaId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ConciliacionBancaria>()
            .FirstOrDefaultAsync(c => c.CuentaBancariaId == cuentaBancariaId && c.Estado == EstadoConciliacionBancaria.EnProceso, cancellationToken);
    }

    public async Task AddAsync(ConciliacionBancaria conciliacion, CancellationToken cancellationToken = default)
    {
        await _context.Set<ConciliacionBancaria>().AddAsync(conciliacion, cancellationToken);
    }

    public void Update(ConciliacionBancaria conciliacion)
    {
        _context.Set<ConciliacionBancaria>().Update(conciliacion);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IEnumerable<ConciliacionBancaria> Items, int TotalCount)> GetPagedAsync(
        int? cuentaBancariaId,
        EstadoConciliacionBancaria? estado,
        int? mes,
        int? anio,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ConciliacionBancaria>().AsNoTracking();

        if (cuentaBancariaId.HasValue)
            query = query.Where(c => c.CuentaBancariaId == cuentaBancariaId.Value);

        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);

        if (mes.HasValue)
            query = query.Where(c => c.FechaInicio.Month == mes.Value);

        if (anio.HasValue)
            query = query.Where(c => c.FechaInicio.Year == anio.Value);

        query = query.OrderByDescending(c => c.FechaInicio).ThenBy(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
