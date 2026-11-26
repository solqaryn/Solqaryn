using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;
using Solqaryn.Application.Exceptions;

namespace Solqaryn.Infrastructure.Persistence;

internal static class QaCommercialOwnershipModel
{
    internal static IReadOnlyList<QaCommercialOwnershipV1.Owned> OwnedTables { get; } = QaCommercialOwnershipV1.Tables.Concat(QaCommercialOwnershipV2.Tables).ToArray();
    private static IReadOnlyList<QaCommercialOwnershipV1.Edge> Relations { get; } = QaCommercialOwnershipV1.Relations.Concat(QaCommercialOwnershipV2.Relations).ToArray();

    internal static void Configure(ModelBuilder model, AppDbContext db)
    {
        foreach (var entry in OwnedTables)
        {
            var b = model.Entity(entry.Entity);
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName(QaCommercialOwnershipV1.OwnerIndex(entry.Table));
            b.HasOne(typeof(Empresa)).WithMany().HasForeignKey("EmpresaId")
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName(QaCommercialOwnershipV1.OwnerForeignKey(entry.Table));
        }
        model.Entity<ProductoVariante>().HasIndex("EmpresaId", "ProductoId", "Id")
            .IsUnique().HasDatabaseName(QaCommercialOwnershipV1.VariantKey);
        foreach (var edge in Relations)
            model.Entity(OwnedTables.Single(t => t.Table == edge.Child).Entity)
                .HasIndex(edge.Columns).HasDatabaseName(edge.Index);
        foreach (var group in QaCommercialOwnershipV1.NaturalKeys.GroupBy(k => k.Table))
        {
            var b = model.Entity(OwnedTables.Single(t => t.Table == group.Key).Entity);
            b.Property<int>(QaCommercialOwnershipV1.ScopeColumn)
                .HasComputedColumnSql("COALESCE(`EmpresaId`, 0)", stored: true);
            foreach (var key in group)
            {
                var existing = b.Metadata.GetIndexes().Single(i =>
                    i.Properties.Count == 1 && i.Properties[0].Name == key.Column && i.IsUnique);
                b.Metadata.RemoveIndex(existing.Properties);
                b.HasIndex(QaCommercialOwnershipV1.ScopeColumn, key.Column).IsUnique().HasDatabaseName(key.Index);
            }
        }
        Scope<Producto>(model, db);
        Scope<ProductoVariante>(model, db);
        Scope<ProductoImagen>(model, db);
        Scope<Cliente>(model, db);
        Scope<Proveedor>(model, db);
        Scope<Compra>(model, db);
        Scope<Venta>(model, db);
        Scope<CompraDetalle>(model, db);
        Scope<VentaDetalle>(model, db);
        Scope<Factura>(model, db);
        Scope<MovimientoInventario>(model, db);
        Scope<FacturaDetalle>(model, db);
        Scope<FacturaPago>(model, db);
        Scope<CompraDocumento>(model, db);
        Scope<ExistenciaVariante>(model, db);
        Scope<ConsumoInsumo>(model, db);
        Scope<ConsumoInsumoDetalle>(model, db);
        Scope<AjusteInventario>(model, db);
        Scope<AjusteInventarioDetalle>(model, db);
        Scope<TransferenciaInventario>(model, db);
        Scope<TransferenciaInventarioDetalle>(model, db);
        Scope<RecepcionCompra>(model, db);
        Scope<RecepcionCompraDetalle>(model, db);
        Scope<OrdenCompra>(model, db);
        Scope<OrdenCompraDetalle>(model, db);
        Scope<SolicitudCompra>(model, db);
        Scope<SolicitudCompraDetalle>(model, db);
        Scope<CostoEnvio>(model, db);
        Scope<DashboardKpiConfiguracion>(model, db);
        Scope<MovimientoFinanciero>(model, db);
        Scope<RevisionFinanciera>(model, db);
    }

    private static void Scope<TEntity>(ModelBuilder model, AppDbContext db) where TEntity : class =>
        model.Entity<TEntity>().HasQueryFilter(QaCommercialOwnershipV1.TenantFilter,
            row => !db.CommercialRequestRequiresTenant ||
                (db.CommercialReadEmpresaId.HasValue &&
                 EF.Property<int?>(row, "EmpresaId") == db.CommercialReadEmpresaId));

    internal static async Task ValidateWritesAsync(AppDbContext db, CancellationToken ct)
    {
        if (!db.CommercialRequestRequiresTenant) return;
        var entries = db.ChangeTracker.Entries().Where(e =>
            OwnedTables.Any(t => t.Entity == e.Metadata.ClrType) &&
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        if (entries.Length == 0) return;
        var tenant = db.CommercialWriteEmpresaId;
        if (tenant is not > 0) throw new ForbiddenAccessException("No hay autoridad empresarial de escritura.");
        foreach (var entry in entries)
        {
            if (!db.CommercialPrivateWriteAuthorized &&
                (entry.Metadata.ClrType != typeof(Cliente) || entry.State != EntityState.Added))
                throw new ForbiddenAccessException("La autoridad de tienda no permite mutar este recurso.");
            var owner = entry.Property("EmpresaId");
            if (owner.CurrentValue is int supplied && supplied != tenant)
                throw new ForbiddenAccessException("No se puede escribir en otra empresa.");
            if (entry.State == EntityState.Added)
                owner.CurrentValue = tenant;
            else
            {
                var persisted = await entry.GetDatabaseValuesAsync(ct);
                if (persisted is null || persisted["EmpresaId"] is not int persistedOwner || persistedOwner != tenant)
                    throw new ForbiddenAccessException("El recurso no pertenece a la empresa autorizada.");
                owner.OriginalValue = tenant;
                owner.CurrentValue = tenant;
                owner.IsModified = false;
            }
        }

        // All new owners are set first, including tracked principals with
        // temporary IDs. Verify the graph before executing the database write.
        foreach (var entry in entries.Where(e => e.State != EntityState.Deleted))
        {
            var owned = OwnedTables.Single(t => t.Entity == entry.Metadata.ClrType);
            foreach (var edge in Relations.Where(e => e.Child == owned.Table))
            {
                if (edge.Columns.Skip(1).Any(col => entry.Property(col).CurrentValue is null))
                    continue; // nullable reference, never a nullable owner
                var id = entry.Property(edge.Columns[^1]).CurrentValue;
                if (id is not int)
                    throw new ForbiddenAccessException("Referencia comercial inválida.");
                var principal = OwnedTables.Single(t => t.Table == edge.Parent);
                var tracked = db.ChangeTracker.Entries().FirstOrDefault(p => p.Metadata.ClrType == principal.Entity &&
                    Equals(p.Property("Id").CurrentValue, id));
                if (tracked is null)
                {
                    var entity = await db.FindAsync(principal.Entity, new object?[] { id }, ct);
                    if (entity is not null) tracked = db.Entry(entity);
                }
                if (tracked is null || tracked.State == EntityState.Deleted ||
                    tracked.Property("EmpresaId").CurrentValue is not int ownerId || ownerId != tenant)
                    throw new ForbiddenAccessException("La referencia no pertenece a la empresa autorizada.");
                for (var i = 1; i < edge.Principal.Length - 1; i++)
                    if (!Equals(tracked.Property(edge.Principal[i]).CurrentValue, entry.Property(edge.Columns[i]).CurrentValue))
                        throw new ForbiddenAccessException("La variante no pertenece al producto indicado.");
            }
            foreach (var field in new[] { "AlmacenId", "AlmacenOrigenId", "AlmacenDestinoId" })
            {
                if (entry.Metadata.FindProperty(field) is null || entry.Property(field).CurrentValue is not int almacenId)
                    continue;
                if (!await db.Set<Almacen>().AnyAsync(a => a.Id == almacenId && a.Sucursal.EmpresaId == tenant, ct))
                    throw new ForbiddenAccessException("El almacén no pertenece a la empresa autorizada.");
            }
        }
    }
}
