using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class CuentaPorPagarRepository : ICuentaPorPagarRepository
{
    private const string FacturaConstraint = "UX_CuentasPorPagar_FacturaProveedorId";
    private const string AplicacionConstraint = "UX_AplicacionesCuentaPorPagar_Cuenta_IdempotencyKey";
    private readonly AppDbContext _context;

    public CuentaPorPagarRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<(IReadOnlyList<CuentaPorPagar> Items, int Total)> GetPagedAsync(CuentaPorPagarFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = _context.Set<CuentaPorPagar>()
            .AsNoTracking()
            .Include(x => x.Aplicaciones)
            .AsQueryable();

        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.CondicionPago.HasValue)
            query = query.Where(x => x.CondicionPago == filtro.CondicionPago.Value);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.FacturaProveedorId.HasValue)
            query = query.Where(x => x.FacturaProveedorId == filtro.FacturaProveedorId.Value);
        if (filtro.VenceDesdeUtc.HasValue)
            query = query.Where(x => x.FechaVencimientoUtc >= filtro.VenceDesdeUtc.Value);
        if (filtro.VenceHastaUtc.HasValue)
            query = query.Where(x => x.FechaVencimientoUtc <= filtro.VenceHastaUtc.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Moneda))
        {
            var moneda = filtro.Moneda.Trim().ToUpperInvariant();
            query = query.Where(x => x.Moneda == moneda);
        }

        var total = await query.CountAsync();
        var desc = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = desc
            ? query.OrderByDescending(x => x.FechaVencimientoUtc).ThenByDescending(x => x.Id)
            : query.OrderBy(x => x.FechaVencimientoUtc).ThenBy(x => x.Id);

        var items = await query
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<CuentaPorPagar?> GetByIdAsync(int id, bool tracking = false)
    {
        var query = _context.Set<CuentaPorPagar>().Include(x => x.Aplicaciones).AsQueryable();
        return tracking
            ? query.AsTracking().FirstOrDefaultAsync(x => x.Id == id)
            : query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CuentaPorPagar?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var entity = await _context.Set<CuentaPorPagar>()
            .FromSqlInterpolated($"SELECT c.* FROM CuentasPorPagar c WHERE c.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (entity is not null)
            await _context.Entry(entity).Collection(x => x.Aplicaciones).LoadAsync();

        return entity;
    }

    public Task<CuentaPorPagar?> GetByFacturaProveedorIdAsync(int facturaProveedorId, bool tracking = false)
    {
        var query = _context.Set<CuentaPorPagar>()
            .Include(x => x.Aplicaciones)
            .Where(x => x.FacturaProveedorId == facturaProveedorId);

        return tracking
            ? query.AsTracking().FirstOrDefaultAsync()
            : query.AsNoTracking().FirstOrDefaultAsync();
    }

    public Task AddAsync(CuentaPorPagar cuenta) =>
        _context.Set<CuentaPorPagar>().AddAsync(cuenta).AsTask();

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, FacturaConstraint))
        {
            throw new UniqueConstraintViolationException(
                FacturaConstraint,
                "La factura de proveedor ya tiene una cuenta por pagar.",
                ex);
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, AplicacionConstraint))
        {
            throw new UniqueConstraintViolationException(
                AplicacionConstraint,
                "La clave de idempotencia ya existe para la cuenta por pagar.",
                ex);
        }
    }

    private static bool ContieneConstraint(DbUpdateException ex, string constraintName)
    {
        var detail = ex.InnerException?.Message ?? ex.Message;
        return detail.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }
}
