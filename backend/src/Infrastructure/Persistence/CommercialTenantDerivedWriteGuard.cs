using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Propaga propiedad empresarial desde padres de confianza a entidades comerciales
/// directas, validando referencias persistidas y evitando re-asignaciones desconectadas.
/// La columna NULL únicamente preserva fixtures anteriores al cutover; la cuarentena
/// temporal se mantiene hasta aislar completamente el grafo.
/// </summary>
public static class CommercialTenantDerivedWriteGuard
{
    public static async Task ValidateAsync(
        AppDbContext db, int? empresaAutorizada, CancellationToken cancellationToken)
    {
        foreach (var entry in db.ChangeTracker.Entries<ProductoVariante>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var value = entry.Entity;
            var owner = await OwnerProductoAsync(db, value.ProductoId, value.Producto, cancellationToken);
            await MatchAsync(entry, owner, empresaAutorizada,
                v => v.EmpresaId, (v, id) => v.EmpresaId = id,
                () => db.ProductoVariantes.IgnoreQueryFilters().AsNoTracking()
                    .Where(v => v.Id == value.Id).Select(v => v.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken));
        }

        foreach (var entry in db.ChangeTracker.Entries<ProductoImagen>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var value = entry.Entity;
            var owner = await OwnerProductoAsync(db, value.ProductoId, value.Producto, cancellationToken);
            await MatchAsync(entry, owner, empresaAutorizada,
                v => v.EmpresaId, (v, id) => v.EmpresaId = id,
                () => db.ProductoImagenes.AsNoTracking()
                    .Where(v => v.Id == value.Id).Select(v => v.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken));
            if (entry.State is not EntityState.Deleted && value.ProductoVarianteId is > 0)
            {
                var varianteProductoId = value.ProductoVariante is not null &&
                    db.Entry(value.ProductoVariante).State == EntityState.Added
                        ? value.ProductoVariante.ProductoId
                        : await db.ProductoVariantes.IgnoreQueryFilters().AsNoTracking()
                            .Where(v => v.Id == value.ProductoVarianteId)
                            .Select(v => (int?)v.ProductoId)
                            .SingleOrDefaultAsync(cancellationToken);
                if (varianteProductoId is > 0 && varianteProductoId != value.ProductoId)
                    throw new BusinessRuleException(
                        "La imagen no pertenece a una variante del producto de la misma empresa.");
            }
        }

        foreach (var entry in db.ChangeTracker.Entries<VentaDetalle>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var value = entry.Entity;
            var owner = await OwnerVentaAsync(db, value.VentaId, value.Venta, cancellationToken);
            await MatchAsync(entry, owner, empresaAutorizada,
                v => v.EmpresaId, (v, id) => v.EmpresaId = id,
                () => db.VentaDetalles.AsNoTracking().Where(v => v.Id == value.Id)
                    .Select(v => v.EmpresaId).SingleOrDefaultAsync(cancellationToken));
        }

        foreach (var entry in db.ChangeTracker.Entries<CompraDetalle>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var value = entry.Entity;
            var owner = await OwnerCompraAsync(db, value.CompraId, value.Compra, cancellationToken);
            await MatchAsync(entry, owner, empresaAutorizada,
                v => v.EmpresaId, (v, id) => v.EmpresaId = id,
                () => db.CompraDetalles.AsNoTracking().Where(v => v.Id == value.Id)
                    .Select(v => v.EmpresaId).SingleOrDefaultAsync(cancellationToken));
        }

        foreach (var entry in db.ChangeTracker.Entries<Factura>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var value = entry.Entity;
            var owner = await OwnerVentaAsync(db, value.VentaId, value.Venta, cancellationToken);
            await MatchAsync(entry, owner, empresaAutorizada,
                v => v.EmpresaId, (v, id) => v.EmpresaId = id,
                () => db.Facturas.AsNoTracking().Where(v => v.Id == value.Id)
                    .Select(v => v.EmpresaId).SingleOrDefaultAsync(cancellationToken));
        }
    }

    private static Task<int?> OwnerProductoAsync(
        AppDbContext db, int id, Producto? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Productos.IgnoreQueryFilters().AsNoTracking()
                .Where(v => v.Id == id).Select(v => v.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }

    private static Task<int?> OwnerVentaAsync(
        AppDbContext db, int id, Venta? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Ventas.IgnoreQueryFilters().AsNoTracking()
                .Where(v => v.Id == id).Select(v => v.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }

    private static Task<int?> OwnerCompraAsync(
        AppDbContext db, int id, Compra? tracked, CancellationToken ct)
    {
        if (tracked is not null && db.Entry(tracked).State == EntityState.Added)
            return Task.FromResult(tracked.EmpresaId);
        return id > 0
            ? db.Compras.IgnoreQueryFilters().AsNoTracking()
                .Where(v => v.Id == id).Select(v => v.EmpresaId).SingleOrDefaultAsync(ct)
            : Task.FromResult(tracked?.EmpresaId);
    }

    private static async Task MatchAsync<T>(
        EntityEntry<T> entry, int? ownerPadre, int? tenantValidado,
        Func<T, int?> getOwner, Action<T, int?> setOwner,
        Func<Task<int?>> getPersisted)
        where T : class
    {
        var current = getOwner(entry.Entity);
        if (entry.State == EntityState.Added && current is null && ownerPadre is > 0)
        {
            current = ownerPadre;
            setOwner(entry.Entity, current);
        }

        if (ownerPadre != current && (ownerPadre is > 0 || current is > 0))
            throw new BusinessRuleException(
                "La propiedad empresarial del dependiente no coincide con su documento o producto.");

        if (tenantValidado is > 0 && current is > 0 && current != tenantValidado)
            throw new ForbiddenAccessException(
                "No puede modificar un dependiente comercial de otra empresa.");

        if (entry.State is EntityState.Modified or EntityState.Deleted)
        {
            if (!Equals(entry.Property("EmpresaId").OriginalValue, current))
                throw new BusinessRuleException(
                    "No es posible reasignar la empresa propietaria de un dependiente comercial.");

            var persisted = await getPersisted();
            if (persisted != current)
                throw new BusinessRuleException(
                    "La empresa persistida del registro no coincide con el cambio solicitado.");
        }
    }
}
