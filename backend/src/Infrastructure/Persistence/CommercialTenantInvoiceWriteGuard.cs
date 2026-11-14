using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Vincula detalle, pago y adjunto al dueño persistido del documento.
/// Los NULL legacy no se atribuyen automáticamente.
/// </summary>
public static class CommercialTenantInvoiceWriteGuard
{
    public static async Task ValidateAsync(AppDbContext db, int? authorized, CancellationToken ct)
    {
        foreach (var entry in db.ChangeTracker.Entries<FacturaDetalle>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var row = entry.Entity;
            var parent = await FacturaOwnerAsync(db, row.FacturaId, row.Factura, ct);
            await ValidateOwnerAsync(entry, parent, authorized,
                x => x.EmpresaId, (x, owner) => x.EmpresaId = owner,
                () => db.FacturaDetalles.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(ct));
            if (entry.State == EntityState.Added && row.ProductoId > 0 && parent is > 0)
            {
                var productOwner = await db.Productos.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.ProductoId).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(ct);
                if (productOwner != parent)
                    throw new BusinessRuleException(
                        "La linea de factura no pertenece a un producto de la misma empresa.");
            }
        }
        foreach (var entry in db.ChangeTracker.Entries<FacturaPago>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var row = entry.Entity;
            var parent = await FacturaOwnerAsync(db, row.FacturaId, row.Factura, ct);
            await ValidateOwnerAsync(entry, parent, authorized,
                x => x.EmpresaId, (x, owner) => x.EmpresaId = owner,
                () => db.FacturaPagos.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(ct));
        }
        foreach (var entry in db.ChangeTracker.Entries<CompraDocumento>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var row = entry.Entity;
            var parent = await CompraOwnerAsync(db, row.CompraId, row.Compra, ct);
            await ValidateOwnerAsync(entry, parent, authorized,
                x => x.EmpresaId, (x, owner) => x.EmpresaId = owner,
                () => db.CompraDocumentos.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(ct));
        }
    }

    private static Task<int?> FacturaOwnerAsync(AppDbContext db, int id, Factura? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Facturas.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id)
                .Select(x => x.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }
    private static Task<int?> CompraOwnerAsync(AppDbContext db, int id, Compra? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Compras.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id)
                .Select(x => x.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }

    private static async Task ValidateOwnerAsync<T>(
        EntityEntry<T> entry, int? parentOwner, int? authorized,
        Func<T, int?> ownerGetter, Action<T, int?> ownerSetter,
        Func<Task<int?>> persistedGetter) where T : class
    {
        var owner = ownerGetter(entry.Entity);
        if (entry.State == EntityState.Added && owner is null && parentOwner is > 0)
        {
            ownerSetter(entry.Entity, parentOwner);
            owner = parentOwner;
        }
        if ((owner is > 0 || parentOwner is > 0) && owner != parentOwner)
            throw new BusinessRuleException(
                "La empresa del documento y la de su dependiente no coinciden.");
        if (authorized is > 0 && owner != authorized)
            throw new ForbiddenAccessException(
                "El documento no pertenece a la empresa autorizada.");
        if (entry.State is EntityState.Modified or EntityState.Deleted)
        {
            if (!Equals(entry.Property("EmpresaId").OriginalValue, owner))
                throw new BusinessRuleException("Prohibido reasignar empresa de documento.");
            if (await persistedGetter() != owner)
                throw new BusinessRuleException("Owner persistido inconsistente.");
        }
    }
}
