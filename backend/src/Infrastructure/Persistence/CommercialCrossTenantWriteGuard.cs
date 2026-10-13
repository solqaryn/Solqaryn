using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Rechaza nuevas asociaciones con propietarios de empresa explicitamente distintos.
/// Una fila legacy con EmpresaId NULL permanece sin atribuir (P0 sigue en cuarentena).
/// Esto no sustituye filtros tenant en las lecturas de repositorios o storefront.
/// </summary>
public static class CommercialCrossTenantWriteGuard
{
    public static async Task ValidateAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        // Sin permisos de reasignación implícita, incluso de NULL legacy a un id.
        CheckOwnerNotReassigned(db);

        foreach (var entry in db.ChangeTracker.Entries<Venta>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var venta = entry.Entity;
            if (venta.EmpresaId is not > 0 || venta.ClienteId is not > 0)
                continue;
            var owner = venta.Cliente?.EmpresaId
                ?? await db.Clientes.IgnoreQueryFilters().AsNoTracking()
                    .Where(c => c.Id == venta.ClienteId.Value)
                    .Select(c => c.EmpresaId).SingleOrDefaultAsync(cancellationToken);
            Check(venta.EmpresaId, owner);
        }

        foreach (var entry in db.ChangeTracker.Entries<Compra>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var compra = entry.Entity;
            if (compra.EmpresaId is not > 0 || compra.ProveedorId is not > 0)
                continue;
            var owner = compra.Proveedor?.EmpresaId
                ?? await db.Proveedores.IgnoreQueryFilters().AsNoTracking()
                    .Where(p => p.Id == compra.ProveedorId.Value)
                    .Select(p => p.EmpresaId).SingleOrDefaultAsync(cancellationToken);
            Check(compra.EmpresaId, owner);
        }

        foreach (var entry in db.ChangeTracker.Entries<VentaDetalle>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var detalle = entry.Entity;
            var ownerVenta = detalle.Venta?.EmpresaId;
            if (ownerVenta is null && detalle.VentaId > 0)
                ownerVenta = await db.Ventas.IgnoreQueryFilters().AsNoTracking()
                    .Where(v => v.Id == detalle.VentaId).Select(v => v.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken);
            if (ownerVenta is not > 0) continue;
            var ownerProducto = detalle.Producto?.EmpresaId;
            if (ownerProducto is null && detalle.ProductoId > 0)
                ownerProducto = await db.Productos.IgnoreQueryFilters().AsNoTracking()
                    .Where(p => p.Id == detalle.ProductoId).Select(p => p.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken);
            Check(ownerVenta, ownerProducto);
        }

        foreach (var entry in db.ChangeTracker.Entries<CompraDetalle>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var detalle = entry.Entity;
            var ownerCompra = detalle.Compra?.EmpresaId;
            if (ownerCompra is null && detalle.CompraId > 0)
                ownerCompra = await db.Compras.IgnoreQueryFilters().AsNoTracking()
                    .Where(c => c.Id == detalle.CompraId).Select(c => c.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken);
            if (ownerCompra is not > 0) continue;
            var ownerProducto = detalle.Producto?.EmpresaId;
            if (ownerProducto is null && detalle.ProductoId > 0)
                ownerProducto = await db.Productos.IgnoreQueryFilters().AsNoTracking()
                    .Where(p => p.Id == detalle.ProductoId).Select(p => p.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken);
            Check(ownerCompra, ownerProducto);
        }
    }

    private static void CheckOwnerNotReassigned(AppDbContext db)
    {
        foreach (var entry in db.ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Modified &&
                        (e.Entity is Producto || e.Entity is Cliente ||
                         e.Entity is Proveedor || e.Entity is Compra ||
                         e.Entity is Venta)))
        {
            var empresa = entry.Property("EmpresaId");
            if (!Equals(empresa.OriginalValue, empresa.CurrentValue))
                throw new BusinessRuleException(
                    "La empresa propietaria de un registro comercial no puede modificarse mediante escrituras ordinarias.");
        }
    }

    private static void Check(int? empresaOrigen, int? empresaReferencia)
    {
        if (empresaOrigen is > 0 && empresaReferencia is > 0 &&
            empresaOrigen != empresaReferencia)
            throw new BusinessRuleException("La referencia comercial pertenece a otra empresa.");
    }
}
