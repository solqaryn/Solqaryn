using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class PreparacionPedidoVentaConfiguration : IEntityTypeConfiguration<PreparacionPedidoVenta>
{
    public void Configure(EntityTypeBuilder<PreparacionPedidoVenta> builder)
    {
        builder.ToTable("PreparacionesPedidoVenta", table =>
        {
            table.HasCheckConstraint(
                "CK_PreparacionesPedidoVenta_Estado",
                $"`Estado` IN ({(int)EstadoPreparacionPedidoVenta.PendientePicking}, {(int)EstadoPreparacionPedidoVenta.PickingCompletado}, {(int)EstadoPreparacionPedidoVenta.PackingCompletado}, {(int)EstadoPreparacionPedidoVenta.Despachado}, {(int)EstadoPreparacionPedidoVenta.Entregado}, {(int)EstadoPreparacionPedidoVenta.Cancelado})");
        });

        builder.Property(p => p.Estado).HasConversion<int>().IsRequired();
        builder.Property(p => p.MotivoCancelacion).HasMaxLength(1000);

        builder.HasIndex(p => p.PedidoVentaId).IsUnique().HasDatabaseName("UX_PreparacionesPedidoVenta_PedidoVentaId");
        builder.HasIndex(p => p.ReservaInventarioId).IsUnique().HasDatabaseName("UX_PreparacionesPedidoVenta_ReservaInventarioId");
        builder.HasIndex(p => p.Estado).HasDatabaseName("IX_PreparacionesPedidoVenta_Estado");

        builder.HasOne(p => p.PedidoVenta).WithMany().HasForeignKey(p => p.PedidoVentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.ReservaInventario).WithMany().HasForeignKey(p => p.ReservaInventarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Detalles).WithOne(d => d.PreparacionPedidoVenta).HasForeignKey(d => d.PreparacionPedidoVentaId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PreparacionPedidoVentaDetalleConfiguration : IEntityTypeConfiguration<PreparacionPedidoVentaDetalle>
{
    public void Configure(EntityTypeBuilder<PreparacionPedidoVentaDetalle> builder)
    {
        builder.ToTable("PreparacionPedidoVentaDetalles", table =>
        {
            table.HasCheckConstraint("CK_PreparacionPedidoVentaDetalles_CantidadPreparar", "`CantidadPreparar` > 0");
        });

        builder.Property(p => p.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(p => p.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(p => p.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(p => p.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(p => p.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property<int>("UbicacionNormalizada")
            .HasComputedColumnSql("COALESCE(`UbicacionAlmacenId`, 0)", stored: true);

        builder.HasIndex(new[]
            {
                nameof(PreparacionPedidoVentaDetalle.PreparacionPedidoVentaId),
                nameof(PreparacionPedidoVentaDetalle.ProductoVarianteId),
                nameof(PreparacionPedidoVentaDetalle.AlmacenId),
                "UbicacionNormalizada"
            })
            .IsUnique()
            .HasDatabaseName("UX_PreparacionPedidoVentaDetalles_ClaveFisica");
        builder.HasIndex(p => p.ProductoVarianteId).HasDatabaseName("IX_PreparacionPedidoVentaDetalles_ProductoVarianteId");
        builder.HasIndex(p => p.AlmacenId).HasDatabaseName("IX_PreparacionPedidoVentaDetalles_AlmacenId");

        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(p => p.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Almacen>()
            .WithMany()
            .HasForeignKey(p => p.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UbicacionAlmacen>()
            .WithMany()
            .HasForeignKey(p => new { p.AlmacenId, p.UbicacionAlmacenId })
            .HasPrincipalKey(p => new { p.AlmacenId, p.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
