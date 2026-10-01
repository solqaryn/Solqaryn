using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class FacturaProveedorRepository : IFacturaProveedorRepository
{
    private const string NumeroConstraint = "UX_FacturasProveedor_Proveedor_NumeroFactura";
    private readonly AppDbContext _context;

    public FacturaProveedorRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<FacturaProveedor> ConDetalles(bool tracking = false)
    {
        var query = _context.Set<FacturaProveedor>()
            .Include(x => x.Detalles)
            .AsSplitQuery();
        return tracking ? query.AsTracking() : query.AsNoTracking();
    }

    public async Task<(IReadOnlyList<FacturaProveedor> Items, int Total)> GetPagedAsync(FacturaProveedorFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = _context.Set<FacturaProveedor>().AsNoTracking().AsQueryable();

        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.OrdenCompraId.HasValue)
            query = query.Where(x => x.OrdenCompraId == filtro.OrdenCompraId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Numero))
        {
            var numero = filtro.Numero.Trim();
            query = query.Where(x => x.NumeroFactura.Contains(numero));
        }
        if (filtro.Desde.HasValue)
            query = query.Where(x => x.FechaEmisionUtc >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(x => x.FechaEmisionUtc <= filtro.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(x => x.NumeroFactura.Contains(search)
                || x.ProveedorNombreSnapshot.Contains(search)
                || (x.ReferenciaFiscal != null && x.ReferenciaFiscal.Contains(search)));
        }

        var total = await query.CountAsync();
        var desc = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = filtro.SortBy?.Trim().ToLowerInvariant() switch
        {
            "numerofactura" or "numero" => desc ? query.OrderByDescending(x => x.NumeroFactura) : query.OrderBy(x => x.NumeroFactura),
            "estado" => desc ? query.OrderByDescending(x => x.Estado) : query.OrderBy(x => x.Estado),
            "proveedor" => desc ? query.OrderByDescending(x => x.ProveedorNombreSnapshot) : query.OrderBy(x => x.ProveedorNombreSnapshot),
            "fechavencimientoutc" or "fechavencimiento" => desc ? query.OrderByDescending(x => x.FechaVencimientoUtc) : query.OrderBy(x => x.FechaVencimientoUtc),
            _ => desc ? query.OrderByDescending(x => x.FechaEmisionUtc) : query.OrderBy(x => x.FechaEmisionUtc)
        };

        var ids = await query
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(x => x.Id)
            .ToListAsync();

        if (ids.Count == 0)
            return (Array.Empty<FacturaProveedor>(), total);

        var byId = await ConDetalles()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        var items = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        return (items, total);
    }

    public Task<FacturaProveedor?> GetByIdAsync(int id, bool tracking = false) =>
        ConDetalles(tracking).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<FacturaProveedor?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var factura = await _context.Set<FacturaProveedor>()
            .FromSqlInterpolated($"SELECT f.* FROM FacturasProveedor f WHERE f.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
        if (factura is not null)
            await _context.Entry(factura).Collection(x => x.Detalles).LoadAsync();
        return factura;
    }

    public Task<FacturaProveedor?> GetByProveedorNumeroAsync(int proveedorId, string numeroFactura, bool tracking = false)
    {
        var numero = numeroFactura.Trim().ToUpperInvariant();
        return ConDetalles(tracking)
            .FirstOrDefaultAsync(x => x.ProveedorId == proveedorId && x.NumeroFactura == numero);
    }

    public async Task<decimal> GetCantidadRegistradaAcumuladaPorDetalleAsync(
        int ordenCompraDetalleId,
        int? excluirFacturaId = null)
    {
        var query = _context.Set<FacturaProveedorDetalle>()
            .AsNoTracking()
            .Where(x => x.OrdenCompraDetalleId == ordenCompraDetalleId
                && x.FacturaProveedor.Estado == EstadoFacturaProveedor.Registrada);

        if (excluirFacturaId.HasValue)
            query = query.Where(x => x.FacturaProveedorId != excluirFacturaId.Value);

        return await query.SumAsync(x => x.CantidadFacturada);
    }

    public Task AddAsync(FacturaProveedor factura) => _context.Set<FacturaProveedor>().AddAsync(factura).AsTask();

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, NumeroConstraint))
        {
            throw new UniqueConstraintViolationException(
                NumeroConstraint,
                "El número de factura ya existe para el proveedor indicado.",
                ex);
        }
    }

    private static bool ContieneConstraint(DbUpdateException ex, string constraintName)
    {
        var detalle = ex.InnerException?.Message ?? ex.Message;
        return detalle.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }
}
