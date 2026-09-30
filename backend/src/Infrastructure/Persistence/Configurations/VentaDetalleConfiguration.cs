using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class VentaDetalleConfiguration : IEntityTypeConfiguration<VentaDetalle>
{
    public void Configure(EntityTypeBuilder<VentaDetalle> builder)
    {
        builder.ToTable("VentaDetalles", table =>
            table.HasCheckConstraint(
                "CK_VentaDetalles_Ubicacion_RequiereAlmacen",
                "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL"));
        builder.Property(d => d.PrecioUnitario).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoUnitarioSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(d => d.UtilidadBruta).HasColumnType("decimal(18,2)");
        builder.Property(d => d.ProductoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(d => d.ProductoMarcaSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoModeloSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoSkuSnapshot).HasMaxLength(80);

        builder.HasIndex(d => d.ProductoId)
            .HasDatabaseName("IX_VentaDetalles_ProductoId");
        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ProductoVarianteId);
        builder.HasOne(d => d.ProductoVariante)
            .WithMany()
            .HasForeignKey(d => d.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.AlmacenId, d.UbicacionAlmacenId })
            .HasDatabaseName("IX_VentaDetalles_Almacen_Ubicacion");
        builder.HasOne(d => d.Almacen)
            .WithMany()
            .HasForeignKey(d => d.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VentaDetalles_Almacenes_AlmacenId_N14");
        builder.HasOne(d => d.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(d => new { d.AlmacenId, d.UbicacionAlmacenId })
            .HasPrincipalKey(u => new { u.AlmacenId, u.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VentaDetalles_Ubicacion_MismoAlmacen_N14");
    }
}
