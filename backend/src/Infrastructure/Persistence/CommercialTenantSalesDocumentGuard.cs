using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Atribuye nuevos documentos de cotización/pedido y líneas a padres verificables;
/// rechaza transacciones con cliente, producto, variante o documento de otro tenant.
/// Nunca interpreta identidad del DTO/header ni atribuye NULL históricos.
/// </summary>
public static class CommercialTenantSalesDocumentGuard
{
    public static async Task ValidateAsync(AppDbContext db, int? authorized, CancellationToken ct)
    {
        foreach (var entry in db.ChangeTracker.Entries<Cotizacion>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var row = entry.Entity;
            var owner = await ClientOwnerAsync(db, row.ClienteId, row.Cliente, ct);
            await MatchAsync(entry, owner, authorized, x => x.EmpresaId,
                (x, e) => x.EmpresaId = e,
                () => db.Cotizaciones.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.Id).Select(x => x.EmpresaId).SingleOrDefaultAsync(ct));
        }

        foreach (var entry in db.ChangeTracker.Entries<PedidoVenta>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var row = entry.Entity;
            var owner = await ClientOwnerAsync(db, row.ClienteId, row.Cliente, ct);
            if (row.CotizacionId is > 0 || row.Cotizacion is not null)
            {
                var quotationOwner = await QuoteOwnerAsync(db, row.CotizacionId ?? 0, row.Cotizacion, ct);
                CrossReference(owner, quotationOwner);
            }
            await MatchAsync(entry, owner, authorized, x => x.EmpresaId,
                (x, e) => x.EmpresaId = e,
                () => db.PedidosVenta.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.Id).Select(x => x.EmpresaId).SingleOrDefaultAsync(ct));
        }

        foreach (var entry in db.ChangeTracker.Entries<CotizacionDetalle>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var row = entry.Entity;
            var owner = await QuoteOwnerAsync(db, row.CotizacionId, row.Cotizacion, ct);
            await MatchAsync(entry, owner, authorized, x => x.EmpresaId,
                (x, e) => x.EmpresaId = e,
                () => db.CotizacionDetalles.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.Id).Select(x => x.EmpresaId).SingleOrDefaultAsync(ct));
            if (entry.State is not EntityState.Deleted)
                await ValidateProductAndVariantAsync(db, owner, row.ProductoId, row.Producto,
                    row.ProductoVarianteId, row.ProductoVariante, ct);
        }

        foreach (var entry in db.ChangeTracker.Entries<PedidoVentaDetalle>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var row = entry.Entity;
            var owner = await OrderOwnerAsync(db, row.PedidoVentaId, row.PedidoVenta, ct);
            await MatchAsync(entry, owner, authorized, x => x.EmpresaId,
                (x, e) => x.EmpresaId = e,
                () => db.PedidoVentaDetalles.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == row.Id).Select(x => x.EmpresaId).SingleOrDefaultAsync(ct));
            if (entry.State is not EntityState.Deleted)
                await ValidateProductAndVariantAsync(db, owner, row.ProductoId, row.Producto,
                    row.ProductoVarianteId, row.ProductoVariante, ct);
        }
    }

    private static async Task ValidateProductAndVariantAsync(
        AppDbContext db, int? owner, int productId, Producto? product,
        int? variantId, ProductoVariante? variant, CancellationToken ct)
    {
        var productOwner = await ProductOwnerAsync(db, productId, product, ct);
        CrossReference(owner, productOwner);
        if (variantId is > 0 || variant is not null)
        {
            int? variantOwner;
            int? variantProduct;
            if (variant is not null && db.Entry(variant).State == EntityState.Added)
            {
                variantOwner = variant.EmpresaId;
                variantProduct = variant.ProductoId > 0 ? variant.ProductoId : null;
            }
            else
            {
                var saved = await db.ProductoVariantes.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == variantId).Select(x => new { x.EmpresaId, x.ProductoId })
                    .SingleOrDefaultAsync(ct);
                variantOwner = saved?.EmpresaId;
                variantProduct = saved?.ProductoId;
            }
            CrossReference(owner, variantOwner);
            if (productId > 0 && variantProduct != productId)
                throw new BusinessRuleException(
                    "La variante no pertenece al producto del documento comercial.");
        }
    }

    private static Task<int?> ClientOwnerAsync(AppDbContext db, int id, Cliente? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Clientes.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id)
                .Select(x => x.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }
    private static Task<int?> ProductOwnerAsync(AppDbContext db, int id, Producto? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Productos.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id)
                .Select(x => x.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }
    private static Task<int?> QuoteOwnerAsync(AppDbContext db, int id, Cotizacion? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Cotizaciones.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id)
                .Select(x => x.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }
    private static Task<int?> OrderOwnerAsync(AppDbContext db, int id, PedidoVenta? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.PedidosVenta.IgnoreQueryFilters().AsNoTracking().Where(x => x.Id == id)
                .Select(x => x.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }

    private static void CrossReference(int? source, int? reference)
    {
        if (source != reference && (source is > 0 || reference is > 0))
            throw new BusinessRuleException(
                "Documento comercial con referencias de empresas distintas o sin dueño verificado.");
    }

    private static async Task MatchAsync<T>(
        EntityEntry<T> entry, int? parentOwner, int? authorized,
        Func<T, int?> get, Action<T, int?> set, Func<Task<int?>> persisted)
        where T : class
    {
        var owner = get(entry.Entity);
        if (entry.State == EntityState.Added && owner is null && parentOwner is > 0)
        {
            owner = parentOwner;
            set(entry.Entity, owner);
        }
        CrossReference(owner, parentOwner);
        if (authorized is > 0 && owner != authorized)
            throw new ForbiddenAccessException(
                "No puede modificar un documento comercial de otra empresa.");
        if (entry.State is EntityState.Modified or EntityState.Deleted)
        {
            if (!Equals(entry.Property("EmpresaId").OriginalValue, owner)
                || await persisted() != owner)
                throw new BusinessRuleException(
                    "No se permite alterar la propiedad empresarial persistida.");
        }
    }
}
