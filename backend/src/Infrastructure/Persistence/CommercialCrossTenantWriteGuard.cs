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
    public static async Task ValidateAsync(
        AppDbContext db, CancellationToken cancellationToken,
        int? empresaAutorizada = null)
    {
        if (empresaAutorizada is > 0)
            CheckAuthorizedTenantWrites(db, empresaAutorizada.Value);

        // Sin permisos de reasignación implícita, incluso de NULL legacy a un id.
        await CheckOwnerNotReassignedAsync(db, cancellationToken);

        foreach (var entry in db.ChangeTracker.Entries<Venta>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var venta = entry.Entity;
            if (venta.ClienteId is not > 0 && venta.Cliente is null)
                continue;
            // Nunca confiar en la navegación cuando existe un FK persistido.
            // Una entidad nueva enlazada en el mismo SaveChanges aún no existe
            // en MySQL. Solo puede confiarse en ella si EF la rastrea como Added.
            var owner = venta.Cliente is not null &&
                        db.Entry(venta.Cliente).State == EntityState.Added
                ? venta.Cliente.EmpresaId
                : venta.ClienteId is > 0
                    ? await db.Clientes.IgnoreQueryFilters().AsNoTracking()
                        .Where(c => c.Id == venta.ClienteId.Value)
                        .Select(c => c.EmpresaId).SingleOrDefaultAsync(cancellationToken)
                    : venta.Cliente?.EmpresaId;
            Check(venta.EmpresaId, owner);
        }

        foreach (var entry in db.ChangeTracker.Entries<Compra>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var compra = entry.Entity;
            if (compra.ProveedorId is not > 0 && compra.Proveedor is null)
                continue;
            var owner = compra.Proveedor is not null &&
                        db.Entry(compra.Proveedor).State == EntityState.Added
                ? compra.Proveedor.EmpresaId
                : compra.ProveedorId is > 0
                    ? await db.Proveedores.IgnoreQueryFilters().AsNoTracking()
                        .Where(p => p.Id == compra.ProveedorId.Value)
                        .Select(p => p.EmpresaId).SingleOrDefaultAsync(cancellationToken)
                    : compra.Proveedor?.EmpresaId;
            Check(compra.EmpresaId, owner);
        }

        foreach (var entry in db.ChangeTracker.Entries<VentaDetalle>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var detalle = entry.Entity;
            var ownerVenta = detalle.Venta is not null &&
                             db.Entry(detalle.Venta).State == EntityState.Added
                ? detalle.Venta.EmpresaId
                : detalle.VentaId > 0
                    ? await db.Ventas.IgnoreQueryFilters().AsNoTracking()
                        .Where(v => v.Id == detalle.VentaId).Select(v => v.EmpresaId)
                        .SingleOrDefaultAsync(cancellationToken)
                    : detalle.Venta?.EmpresaId;
            var ownerProducto = detalle.Producto is not null &&
                                db.Entry(detalle.Producto).State == EntityState.Added
                ? detalle.Producto.EmpresaId
                : detalle.ProductoId > 0
                    ? await db.Productos.IgnoreQueryFilters().AsNoTracking()
                        .Where(p => p.Id == detalle.ProductoId).Select(p => p.EmpresaId)
                        .SingleOrDefaultAsync(cancellationToken)
                    : detalle.Producto?.EmpresaId;
            Check(ownerVenta, ownerProducto);
            // Los grafos históricos sin owner siguen en cuarentena.
            if (ownerVenta is > 0 || ownerProducto is > 0)
                await CheckProductoVarianteAsync(db, detalle.ProductoId,
                    detalle.ProductoVarianteId, detalle.ProductoVariante,
                    detalle.Producto, cancellationToken);
        }

        foreach (var entry in db.ChangeTracker.Entries<CompraDetalle>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var detalle = entry.Entity;
            var ownerCompra = detalle.Compra is not null &&
                              db.Entry(detalle.Compra).State == EntityState.Added
                ? detalle.Compra.EmpresaId
                : detalle.CompraId > 0
                    ? await db.Compras.IgnoreQueryFilters().AsNoTracking()
                        .Where(c => c.Id == detalle.CompraId).Select(c => c.EmpresaId)
                        .SingleOrDefaultAsync(cancellationToken)
                    : detalle.Compra?.EmpresaId;
            var ownerProducto = detalle.Producto is not null &&
                                db.Entry(detalle.Producto).State == EntityState.Added
                ? detalle.Producto.EmpresaId
                : detalle.ProductoId > 0
                    ? await db.Productos.IgnoreQueryFilters().AsNoTracking()
                        .Where(p => p.Id == detalle.ProductoId).Select(p => p.EmpresaId)
                        .SingleOrDefaultAsync(cancellationToken)
                    : detalle.Producto?.EmpresaId;
            Check(ownerCompra, ownerProducto);
            if (ownerCompra is > 0 || ownerProducto is > 0)
                await CheckProductoVarianteAsync(db, detalle.ProductoId,
                    detalle.ProductoVarianteId, detalle.ProductoVariante,
                    detalle.Producto, cancellationToken);
        }
    }

    private static void CheckAuthorizedTenantWrites(AppDbContext db, int empresaAutorizada)
    {
        // La marca se emite solo despues de validar membresia/permiso en RBAC.
        // No aceptar que un recurso de B sea cambiado desde una sesion de A,
        // aunque EmpresaId no sea modificado por el request.
        foreach (var entry in db.ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                         (e.Entity is Producto or Cliente or Proveedor or Compra or Venta)))
        {
            if (entry.Property("EmpresaId").CurrentValue is int empresaId &&
                empresaId > 0 && empresaId != empresaAutorizada)
            {
                throw new ForbiddenAccessException(
                    "La operacion comercial no pertenece a la empresa autorizada.");
            }
        }
        // Los registros legacy NULL no se adjudican aqui: la cuarentena P0
        // sigue bloqueando la operacion cuando existen varias empresas.
    }

    private static async Task CheckOwnerNotReassignedAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        // OriginalValue no demuestra ownership al hacer Update(entidadDesconectada):
        // en ese caso puede inicializarse con el valor manipulado por el llamador.
        // Contrastar con la base incluso en DELETE.
        foreach (var entry in db.ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Modified or EntityState.Deleted &&
                        (e.Entity is Producto or Cliente or Proveedor or Compra or Venta)))
        {
            var empresa = entry.Property("EmpresaId");
            if (!Equals(empresa.OriginalValue, empresa.CurrentValue))
                throw new BusinessRuleException(
                    "La empresa propietaria de un registro comercial no puede modificarse mediante escrituras ordinarias.");

            int? persistida = entry.Entity switch
            {
                Producto producto => await db.Productos.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == producto.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken),
                Cliente cliente => await db.Clientes.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == cliente.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken),
                Proveedor proveedor => await db.Proveedores.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == proveedor.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken),
                Compra compra => await db.Compras.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == compra.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken),
                Venta venta => await db.Ventas.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == venta.Id).Select(x => x.EmpresaId)
                    .SingleOrDefaultAsync(cancellationToken),
                _ => null
            };
            if (!Equals(persistida, empresa.CurrentValue))
                throw new BusinessRuleException(
                    "La propiedad empresarial persistida no coincide con la operación comercial.");
        }
    }

    private static async Task CheckProductoVarianteAsync(
        AppDbContext db, int productoId, int? varianteId,
        ProductoVariante? variante, Producto? producto,
        CancellationToken cancellationToken)
    {
        if (variante is not null && db.Entry(variante).State == EntityState.Added)
        {
            if ((productoId > 0 && variante.ProductoId > 0 &&
                 variante.ProductoId != productoId) ||
                (producto is not null && variante.Producto is not null &&
                 !ReferenceEquals(producto, variante.Producto)))
                throw new BusinessRuleException(
                    "La variante comercial no pertenece al producto del detalle.");
            return;
        }

        if (productoId <= 0 || varianteId is not > 0)
            return;

        var productoVariante = await db.ProductoVariantes.IgnoreQueryFilters()
            .AsNoTracking().Where(v => v.Id == varianteId.Value)
            .Select(v => (int?)v.ProductoId)
            .SingleOrDefaultAsync(cancellationToken);
        if (productoVariante != productoId)
            throw new BusinessRuleException("La variante comercial no pertenece al producto del detalle.");
    }

    private static void Check(int? empresaOrigen, int? empresaReferencia)
    {
        // Referenciar filas con dueño explícito y filas sin atribuir también
        // es inseguro; no asignar automáticamente esas filas legacy.
        if (empresaOrigen != empresaReferencia &&
            (empresaOrigen is > 0 || empresaReferencia is > 0))
            throw new BusinessRuleException("La referencia comercial pertenece a otra empresa o no tiene dueño verificado.");
    }
}
