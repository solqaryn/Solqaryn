using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ConsumoInsumoDetalleConfiguration : IEntityTypeConfiguration<ConsumoInsumoDetalle>
{
    public void Configure(EntityTypeBuilder<ConsumoInsumoDetalle> builder)
    {
        builder.ToTable("ConsumoInsumoDetalles", table =>
            table.HasCheckConstraint(
                "CK_ConsumoInsumoDetalles_Ubicacion_RequiereAlmacen",
                "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL"));
        builder.Property(d => d.CostoUnitarioSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.CostoTotalSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.NombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(d => d.SkuSnapshot).HasMaxLength(80);
        builder.Property(d => d.ColorSnapshot).HasMaxLength(100);
        builder.HasIndex(d => d.ConsumoInsumoId);
        builder.HasIndex(d => new { d.ProductoId, d.ProductoVarianteId });

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ProductoVariante)
            .WithMany()
            .HasForeignKey(d => d.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.AlmacenId, d.UbicacionAlmacenId })
            .HasDatabaseName("IX_ConsumoInsumoDetalles_Almacen_Ubicacion");
        builder.HasOne(d => d.Almacen)
            .WithMany()
            .HasForeignKey(d => d.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConsumoInsumoDetalles_Almacenes_AlmacenId_N14");
        builder.HasOne(d => d.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(d => new { d.AlmacenId, d.UbicacionAlmacenId })
            .HasPrincipalKey(u => new { u.AlmacenId, u.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConsumoInsumoDetalles_Ubicacion_MismoAlmacen_N14");
    }
}
