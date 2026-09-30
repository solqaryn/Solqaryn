using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class DevolucionProveedorRepository : IDevolucionProveedorRepository
{
    private const string IdempotencyConstraint = "UX_DevolucionesProveedor_IdempotencyKey";
    private const string NumeroConstraint = "UX_DevolucionesProveedor_NumeroDevolucion";
    private readonly AppDbContext _context;

    public DevolucionProveedorRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<DevolucionProveedor> ConDetalles(bool tracking = false)
    {
        var query = _context.Set<DevolucionProveedor>()
            .Include(x => x.Detalles)
            .AsSplitQuery();
        return tracking ? query.AsTracking() : query.AsNoTracking();
    }

    public async Task<(IReadOnlyList<DevolucionProveedor> Items, int Total)> GetPagedAsync(DevolucionProveedorQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = _context.Set<DevolucionProveedor>().AsNoTracking().AsQueryable();

        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.OrdenCompraId.HasValue)
            query = query.Where(x => x.OrdenCompraId == filtro.OrdenCompraId.Value);
        if (filtro.RecepcionCompraId.HasValue)
            query = query.Where(x => x.RecepcionCompraId == filtro.RecepcionCompraId.Value);
        if (filtro.FacturaProveedorId.HasValue)
            query = query.Where(x => x.FacturaProveedorId == filtro.FacturaProveedorId.Value);
        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.DesdeUtc.HasValue)
            query = query.Where(x => x.FechaCreacion >= filtro.DesdeUtc.Value);
        if (filtro.HastaUtc.HasValue)
            query = query.Where(x => x.FechaCreacion <= filtro.HastaUtc.Value);

        var total = await query.CountAsync();
        var ids = await query
            .OrderByDescending(x => x.FechaCreacion)
            .ThenByDescending(x => x.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(x => x.Id)
            .ToListAsync();

        if (ids.Count == 0)
            return (Array.Empty<DevolucionProveedor>(), total);

        var byId = await ConDetalles()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        return (ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList(), total);
    }

    public Task<DevolucionProveedor?> GetByIdAsync(int id, bool tracking = false) =>
        ConDetalles(tracking).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<DevolucionProveedor?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var devolucion = await _context.Set<DevolucionProveedor>()
            .FromSqlInterpolated($"SELECT d.* FROM DevolucionesProveedor d WHERE d.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (devolucion is not null)
            await _context.Entry(devolucion).Collection(x => x.Detalles).LoadAsync();

        return devolucion;
    }

    public Task<DevolucionProveedor?> GetByIdempotencyKeyAsync(string idempotencyKey, bool tracking = false) =>
        ConDetalles(tracking).FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);

    public async Task<decimal> GetCantidadConfirmadaDevueltaPorDetalleAsync(
        int recepcionCompraDetalleId,
        int? excluirDevolucionId = null)
    {
        var query =
            from detalle in _context.Set<DevolucionProveedorDetalle>().AsNoTracking()
            join devolucion in _context.Set<DevolucionProveedor>().AsNoTracking()
                on detalle.DevolucionProveedorId equals devolucion.Id
            where detalle.RecepcionCompraDetalleId == recepcionCompraDetalleId
                  && devolucion.Estado == EstadoDevolucionProveedor.Confirmada
            select new { detalle.Cantidad, devolucion.Id };

        if (excluirDevolucionId.HasValue)
            query = query.Where(x => x.Id != excluirDevolucionId.Value);

        return await query.SumAsync(x => x.Cantidad);
    }

    public async Task<decimal> GetCantidadConfirmadaDevueltaPorFacturaLineaAsync(
        int facturaProveedorId,
        int ordenCompraDetalleId,
        int? excluirDevolucionId = null)
    {
        var query =
            from detalle in _context.Set<DevolucionProveedorDetalle>().AsNoTracking()
            join devolucion in _context.Set<DevolucionProveedor>().AsNoTracking()
                on detalle.DevolucionProveedorId equals devolucion.Id
            where devolucion.FacturaProveedorId == facturaProveedorId
                  && detalle.OrdenCompraDetalleId == ordenCompraDetalleId
                  && devolucion.Estado == EstadoDevolucionProveedor.Confirmada
            select new { detalle.Cantidad, devolucion.Id };

        if (excluirDevolucionId.HasValue)
            query = query.Where(x => x.Id != excluirDevolucionId.Value);

        return await query.SumAsync(x => x.Cantidad);
    }

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null) =>
        _context.Set<DevolucionProveedor>().AsNoTracking()
            .AnyAsync(x => x.NumeroDevolucion == numero && (!excluirId.HasValue || x.Id != excluirId.Value));

    public Task AddAsync(DevolucionProveedor devolucion) =>
        _context.Set<DevolucionProveedor>().AddAsync(devolucion).AsTask();

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, IdempotencyConstraint))
        {
            throw new UniqueConstraintViolationException(
                IdempotencyConstraint,
                "La clave de idempotencia ya fue utilizada por otra devolución concurrente.",
                ex);
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, NumeroConstraint))
        {
            throw new UniqueConstraintViolationException(
                NumeroConstraint,
                "El número de devolución a proveedor ya existe.",
                ex);
        }
    }

    private static bool ContieneConstraint(DbUpdateException ex, string constraintName)
    {
        var detalle = ex.InnerException?.Message ?? ex.Message;
        return detalle.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }
}
