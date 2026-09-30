using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class NotaCreditoProveedorRepository : INotaCreditoProveedorRepository
{
    private const string NumeroConstraint = "UX_NotasCreditoProveedor_Proveedor_Numero";
    private readonly AppDbContext _context;

    public NotaCreditoProveedorRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<(IReadOnlyList<NotaCreditoProveedor> Items, int Total)> GetPagedAsync(
        NotaCreditoProveedorFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = _context.Set<NotaCreditoProveedor>().AsNoTracking().AsQueryable();

        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.FacturaProveedorId.HasValue)
            query = query.Where(x => x.FacturaProveedorId == filtro.FacturaProveedorId.Value);
        if (filtro.DevolucionProveedorId.HasValue)
            query = query.Where(x => x.DevolucionProveedorId == filtro.DevolucionProveedorId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Numero))
        {
            var numero = filtro.Numero.Trim();
            query = query.Where(x => x.NumeroNotaCredito.Contains(numero));
        }
        if (filtro.Desde.HasValue)
            query = query.Where(x => x.FechaEmisionUtc >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(x => x.FechaEmisionUtc <= filtro.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(x =>
                x.NumeroNotaCredito.Contains(search)
                || x.ProveedorNombreSnapshot.Contains(search)
                || x.Motivo.Contains(search)
                || (x.ReferenciaFiscal != null && x.ReferenciaFiscal.Contains(search)));
        }

        var total = await query.CountAsync();
        var desc = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = filtro.SortBy?.Trim().ToLowerInvariant() switch
        {
            "numero" or "numeronotacredito" =>
                desc ? query.OrderByDescending(x => x.NumeroNotaCredito).ThenByDescending(x => x.Id)
                     : query.OrderBy(x => x.NumeroNotaCredito).ThenBy(x => x.Id),
            "estado" =>
                desc ? query.OrderByDescending(x => x.Estado).ThenByDescending(x => x.Id)
                     : query.OrderBy(x => x.Estado).ThenBy(x => x.Id),
            "proveedor" =>
                desc ? query.OrderByDescending(x => x.ProveedorNombreSnapshot).ThenByDescending(x => x.Id)
                     : query.OrderBy(x => x.ProveedorNombreSnapshot).ThenBy(x => x.Id),
            _ =>
                desc ? query.OrderByDescending(x => x.FechaEmisionUtc).ThenByDescending(x => x.Id)
                     : query.OrderBy(x => x.FechaEmisionUtc).ThenBy(x => x.Id)
        };

        var items = await query
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<NotaCreditoProveedor?> GetByIdAsync(int id, bool tracking = false)
    {
        var query = _context.Set<NotaCreditoProveedor>().AsQueryable();
        return tracking
            ? query.AsTracking().FirstOrDefaultAsync(x => x.Id == id)
            : query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<NotaCreditoProveedor?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        return await _context.Set<NotaCreditoProveedor>()
            .FromSqlInterpolated($"SELECT nc.* FROM NotasCreditoProveedor nc WHERE nc.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
    }

    public Task<NotaCreditoProveedor?> GetByProveedorNumeroAsync(
        int proveedorId,
        string numeroNotaCredito,
        bool tracking = false)
    {
        var numero = numeroNotaCredito.Trim().ToUpperInvariant();
        var query = _context.Set<NotaCreditoProveedor>()
            .Where(x => x.ProveedorId == proveedorId && x.NumeroNotaCredito == numero);

        return tracking
            ? query.AsTracking().FirstOrDefaultAsync()
            : query.AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task<decimal> GetCreditoRegistradoAcumuladoPorFacturaAsync(
        int facturaProveedorId,
        int? excluirNotaCreditoId = null)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "GetCreditoRegistradoAcumuladoPorFacturaAsync requiere una transacción activa para serializar el crédito por factura.");

        var factura = await _context.Set<FacturaProveedor>()
            .FromSqlInterpolated($"SELECT f.* FROM FacturasProveedor f WHERE f.Id = {facturaProveedorId} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync()
            ?? throw new ResourceNotFoundException("La factura de proveedor asociada ya no existe.");

        if (factura.Estado != EstadoFacturaProveedor.Registrada)
            throw new BusinessRuleException(
                "La nota de crédito sólo puede registrarse contra una factura de proveedor registrada.");

        if (excluirNotaCreditoId.HasValue)
        {
            var nota = _context.ChangeTracker.Entries<NotaCreditoProveedor>()
                .Where(x => x.Entity.Id == excluirNotaCreditoId.Value)
                .Select(x => x.Entity)
                .SingleOrDefault();

            if (nota?.DevolucionProveedorId is int devolucionId)
            {
                var devolucion = await _context.Set<DevolucionProveedor>()
                    .FromSqlInterpolated($"SELECT d.* FROM DevolucionesProveedor d WHERE d.Id = {devolucionId} FOR UPDATE")
                    .AsTracking()
                    .FirstOrDefaultAsync()
                    ?? throw new BusinessRuleException("La devolución de proveedor vinculada ya no existe.");

                if (devolucion.Estado != EstadoDevolucionProveedor.Confirmada)
                    throw new BusinessRuleException("La devolución vinculada debe permanecer confirmada al registrar la nota de crédito.");
                if (devolucion.FacturaProveedorId != factura.Id || devolucion.ProveedorId != nota.ProveedorId)
                    throw new BusinessRuleException("La devolución vinculada no corresponde a la factura y proveedor indicados.");
            }
        }

        var query = _context.Set<NotaCreditoProveedor>()
            .AsNoTracking()
            .Where(x => x.FacturaProveedorId == facturaProveedorId
                && x.Estado == EstadoNotaCreditoProveedor.Registrada);

        if (excluirNotaCreditoId.HasValue)
            query = query.Where(x => x.Id != excluirNotaCreditoId.Value);

        return await query.SumAsync(x => x.SubtotalCredito + x.ImpuestoCredito);
    }

    public Task AddAsync(NotaCreditoProveedor notaCredito) =>
        _context.Set<NotaCreditoProveedor>().AddAsync(notaCredito).AsTask();

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
                "El número de nota de crédito ya existe para el proveedor indicado.",
                ex);
        }
    }

    private static bool ContieneConstraint(DbUpdateException ex, string constraintName)
    {
        var detail = ex.InnerException?.Message ?? ex.Message;
        return detail.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }
}
