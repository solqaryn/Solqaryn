using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Exceptions;
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

    // Los saldos, estados e idempotencia se calculan sobre todo el agregado.
    // Mantener los filtros empresariales de EF en cabecera, movimientos y matches.
    private IQueryable<ConciliacionBancaria> ConMovimientos() =>
        _context.Set<ConciliacionBancaria>()
            .Include(c => c.Movimientos)
            .ThenInclude(m => m.Matches);

    public async Task<ConciliacionBancaria?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ConMovimientos()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<ConciliacionBancaria?> GetByPeriodoAsync(int cuentaBancariaId, int mes, int anio, CancellationToken cancellationToken = default)
    {
        // Serializar la reapertura sobre la cuenta, antes de leer la versión.
        if (_context.Database.CurrentTransaction is not null &&
            _context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
        {
            var tenant = _context.AuthorizedEmpresaId;
            if (_context.CommercialRequestRequiresTenant && tenant is not > 0)
                throw new ForbiddenAccessException("No hay autoridad empresarial de escritura.");
            var ids = _context.CommercialRequestRequiresTenant
                ? await _context.Database.SqlQuery<int>($"SELECT Id AS Value FROM CuentasBancarias WHERE Id = {cuentaBancariaId} AND EmpresaId = {tenant} FOR UPDATE").ToListAsync(cancellationToken)
                : await _context.Database.SqlQuery<int>($"SELECT Id AS Value FROM CuentasBancarias WHERE Id = {cuentaBancariaId} FOR UPDATE").ToListAsync(cancellationToken);
            if (ids.Count != 1) return null;
        }
        return await ConMovimientos()
            .Where(c => c.CuentaBancariaId == cuentaBancariaId && c.FechaInicio.Month == mes && c.FechaInicio.Year == anio)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConciliacionBancaria?> GetActivaByCuentaAsync(int cuentaBancariaId, CancellationToken cancellationToken = default)
    {
        var activas = await ConMovimientos()
            .Where(c => c.CuentaBancariaId == cuentaBancariaId &&
                (c.Estado == EstadoConciliacionBancaria.Borrador ||
                 c.Estado == EstadoConciliacionBancaria.EnProceso))
            .OrderBy(c => c.Id)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (activas.Count > 1)
            throw new BusinessRuleException("Existe más de una conciliación activa para esta cuenta. Debe resolverse la inconsistencia antes de continuar.");
        return activas.SingleOrDefault();
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
            .Include(c => c.Movimientos)
            .ThenInclude(m => m.Matches)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
