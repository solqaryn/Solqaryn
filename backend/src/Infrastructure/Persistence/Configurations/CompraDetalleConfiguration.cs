using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class CompraDetalleConfiguration : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> builder)
    {
        builder.ToTable("CompraDetalles", table =>
            table.HasCheckConstraint(
                "CK_CompraDetalles_Ubicacion_RequiereAlmacen",
                "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL"));
        builder.Property(d => d.CostoUnitario).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoProductoAnteriorSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoProductoNuevoSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoVarianteAnteriorSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoVarianteNuevoSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.ProductoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(d => d.ProductoMarcaSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoModeloSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoSkuSnapshot).HasMaxLength(80);

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
            .HasDatabaseName("IX_CompraDetalles_Almacen_Ubicacion");
        builder.HasOne(d => d.Almacen)
            .WithMany()
            .HasForeignKey(d => d.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CompraDetalles_Almacenes_AlmacenId_N14");
        builder.HasOne(d => d.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(d => new { d.AlmacenId, d.UbicacionAlmacenId })
            .HasPrincipalKey(u => new { u.AlmacenId, u.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CompraDetalles_Ubicacion_MismoAlmacen_N14");
    }
}
