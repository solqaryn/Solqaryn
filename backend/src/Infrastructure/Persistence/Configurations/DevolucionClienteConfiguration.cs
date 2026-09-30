using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class DevolucionClienteConfiguration : IEntityTypeConfiguration<DevolucionCliente>
{
    public void Configure(EntityTypeBuilder<DevolucionCliente> builder)
    {
        builder.ToTable("DevolucionesCliente", table =>
        {
            table.HasCheckConstraint("CK_DevolucionesCliente_VentaId", "`VentaId` > 0");
            table.HasCheckConstraint("CK_DevolucionesCliente_FacturaId", "`FacturaId` IS NULL OR `FacturaId` > 0");
            table.HasCheckConstraint("CK_DevolucionesCliente_Estado", "`Estado` IN (1, 2, 3)");
            table.HasCheckConstraint("CK_DevolucionesCliente_IdempotenciaAtomica", "(`IdempotencyKey` IS NULL AND `IdempotencyFingerprint` IS NULL) OR (`IdempotencyKey` IS NOT NULL AND `IdempotencyFingerprint` IS NOT NULL)");
        });

        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.Observaciones).HasMaxLength(1000);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.IdempotencyFingerprint).HasColumnType("char(64)").HasMaxLength(64);
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(1000);

        builder.HasIndex(x => x.VentaId).HasDatabaseName("IX_DevolucionesCliente_VentaId");
        builder.HasIndex(x => x.FacturaId).HasDatabaseName("IX_DevolucionesCliente_FacturaId");
        builder.HasIndex(x => x.Estado).HasDatabaseName("IX_DevolucionesCliente_Estado");
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_DevolucionesCliente_IdempotencyKey");

        builder.HasOne(x => x.Venta)
            .WithMany()
            .HasForeignKey(x => x.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Factura)
            .WithMany()
            .HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.DevolucionCliente)
            .HasForeignKey(x => x.DevolucionClienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DevolucionClienteDetalleConfiguration : IEntityTypeConfiguration<DevolucionClienteDetalle>
{
    public void Configure(EntityTypeBuilder<DevolucionClienteDetalle> builder)
    {
        builder.ToTable("DevolucionClienteDetalles", table =>
        {
            table.HasCheckConstraint("CK_DevolucionClienteDetalles_VentaDetalleId", "`VentaDetalleId` > 0");
            table.HasCheckConstraint("CK_DevolucionClienteDetalles_ProductoId", "`ProductoId` > 0");
            table.HasCheckConstraint("CK_DevolucionClienteDetalles_ProductoVarianteId", "`ProductoVarianteId` IS NULL OR `ProductoVarianteId` > 0");
            table.HasCheckConstraint("CK_DevolucionClienteDetalles_Cantidades", "`Cantidad` > 0 AND `CantidadVendidaSnapshot` > 0 AND `Cantidad` <= `CantidadVendidaSnapshot`");
            table.HasCheckConstraint("CK_DevolucionClienteDetalles_Precio", "`PrecioUnitarioSnapshot` >= 0");
            table.HasCheckConstraint("CK_DevolucionClienteDetalles_Resolucion", "`Resolucion` IN (1, 2, 3)");
        });

        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoNombreSnapshot).HasMaxLength(200);
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.PrecioUnitarioSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Resolucion).HasConversion<int>().IsRequired();

        builder.HasIndex(x => new { x.DevolucionClienteId, x.VentaDetalleId })
            .IsUnique()
            .HasDatabaseName("UX_DevolucionClienteDetalles_LineaVenta");
        builder.HasIndex(x => x.VentaDetalleId).HasDatabaseName("IX_DevolucionClienteDetalles_VentaDetalleId");

        builder.HasOne<VentaDetalle>()
            .WithMany()
            .HasForeignKey(x => x.VentaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
