using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Garantiza ownership de snapshots fiscales/descuentos según Compra/Venta
/// persistidas y marca RBAC. No atribuye datos legacy de owner NULL.
/// </summary>
public static class CommercialTenantTaxDiscountWriteGuard
{
    public static async Task ValidateAsync(AppDbContext db, int? authorized, CancellationToken ct)
    {
        foreach (var e in db.ChangeTracker.Entries<VentaImpuesto>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var owner = await ParentOwnerAsync<Venta>(db, e.Property(x => x.VentaId).CurrentValue, ct);
            await CheckAsync(e, owner, authorized, x => x.EmpresaId, (x, o) => x.EmpresaId = o,
                () => StoredOwnerAsync<VentaImpuesto>(db, e.Entity.Id, ct));
        }
        foreach (var e in db.ChangeTracker.Entries<CompraImpuesto>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var owner = await ParentOwnerAsync<Compra>(db, e.Property(x => x.CompraId).CurrentValue, ct);
            await CheckAsync(e, owner, authorized, x => x.EmpresaId, (x, o) => x.EmpresaId = o,
                () => StoredOwnerAsync<CompraImpuesto>(db, e.Entity.Id, ct));
        }
        foreach (var e in db.ChangeTracker.Entries<VentaDescuento>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var owner = await ParentOwnerAsync<Venta>(db, e.Property(x => x.VentaId).CurrentValue, ct);
            await CheckAsync(e, owner, authorized, x => x.EmpresaId, (x, o) => x.EmpresaId = o,
                () => StoredOwnerAsync<VentaDescuento>(db, e.Entity.Id, ct));
        }
        foreach (var e in db.ChangeTracker.Entries<HistorialUsoDescuento>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var owner = await ParentOwnerAsync<Venta>(db, e.Property(x => x.VentaId).CurrentValue, ct);
            if (e.Entity.ClienteId is > 0)
            {
                var clientOwner = await ParentOwnerAsync<Cliente>(db, e.Entity.ClienteId.Value, ct);
                if (owner != clientOwner && (owner is > 0 || clientOwner is > 0))
                    throw new BusinessRuleException("Historial de descuento y cliente pertenecen a empresas distintas.");
            }
            await CheckAsync(e, owner, authorized, x => x.EmpresaId, (x, o) => x.EmpresaId = o,
                () => StoredOwnerAsync<HistorialUsoDescuento>(db, e.Entity.Id, ct));
        }
    }

    private static async Task<int?> ParentOwnerAsync<T>(AppDbContext db, int id, CancellationToken ct)
        where T : class
    {
        var tracked = db.ChangeTracker.Entries<T>()
            .FirstOrDefault(e => e.State != EntityState.Deleted &&
                                 e.Property<int>("Id").CurrentValue == id);
        if (tracked is not null)
            return tracked.Property<int?>("EmpresaId").CurrentValue;
        if (id <= 0)
            return null;
        return await db.Set<T>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => EF.Property<int>(x, "Id") == id)
            .Select(x => EF.Property<int?>(x, "EmpresaId"))
            .SingleOrDefaultAsync(ct);
    }

    private static Task<int?> StoredOwnerAsync<T>(AppDbContext db, int id, CancellationToken ct)
        where T : class =>
        db.Set<T>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => EF.Property<int>(x, "Id") == id)
            .Select(x => EF.Property<int?>(x, "EmpresaId"))
            .SingleOrDefaultAsync(ct);

    private static async Task CheckAsync<T>(
        EntityEntry<T> entry, int? expected, int? authorized,
        Func<T, int?> get, Action<T, int?> set, Func<Task<int?>> persisted) where T : class
    {
        var current = get(entry.Entity);
        if (entry.State == EntityState.Added && current is null && expected is > 0)
        {
            current = expected;
            set(entry.Entity, current);
        }
        if (current != expected && (current is > 0 || expected is > 0))
            throw new BusinessRuleException("El impuesto/descuento no pertenece al mismo tenant del documento.");
        if (authorized is > 0 && current != authorized)
            throw new ForbiddenAccessException("Empresa no autorizada para modificar el documento fiscal.");
        if (entry.State is EntityState.Modified or EntityState.Deleted)
        {
            if (!Equals(entry.Property("EmpresaId").OriginalValue, current) ||
                await persisted() != current)
                throw new BusinessRuleException("Propiedad empresarial del documento no puede reasignarse.");
        }
    }
}
