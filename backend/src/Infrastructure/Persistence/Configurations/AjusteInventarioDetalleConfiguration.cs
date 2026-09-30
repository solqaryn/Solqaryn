using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class AjusteInventarioDetalleConfiguration : IEntityTypeConfiguration<AjusteInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<AjusteInventarioDetalle> builder)
    {
        builder.ToTable("AjusteInventarioDetalles", table =>
            table.HasCheckConstraint(
                "CK_AjusteInventarioDetalles_Ubicacion_RequiereAlmacen",
                "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL"));
        builder.Property(d => d.CostoUnitarioSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(d => d.NombreSnapshot).HasMaxLength(150);
        builder.Property(d => d.SkuSnapshot).HasMaxLength(80);
        builder.Property(d => d.MarcaSnapshot).HasMaxLength(100);
        builder.Property(d => d.ModeloSnapshot).HasMaxLength(100);
        builder.Property(d => d.ColorSnapshot).HasMaxLength(100);
        builder.Property(d => d.TallaSnapshot).HasMaxLength(100);

        builder.Ignore(d => d.DiferenciaSnapshot);
        builder.Ignore(d => d.ImpactoCostoSnapshot);
        builder.Ignore(d => d.TieneSnapshotConfirmacion);

        builder.HasIndex(d => d.AjusteInventarioId)
            .HasDatabaseName("IX_AjusteInventarioDetalles_AjusteInventarioId");
        builder.HasIndex(d => new { d.ProductoId, d.ProductoVarianteId })
            .HasDatabaseName("IX_AjusteInventarioDetalles_Producto_Variante");

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AjusteInventarioDetalles_Productos");

        builder.HasOne(d => d.ProductoVariante)
            .WithMany()
            .HasForeignKey(d => d.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AjusteInventarioDetalles_ProductoVariantes");

        builder.HasIndex(d => new { d.AlmacenId, d.UbicacionAlmacenId })
            .HasDatabaseName("IX_AjusteInventarioDetalles_Almacen_Ubicacion");
        builder.HasOne(d => d.Almacen)
            .WithMany()
            .HasForeignKey(d => d.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AjusteInventarioDetalles_Almacenes_AlmacenId_N14");
        builder.HasOne(d => d.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(d => new { d.AlmacenId, d.UbicacionAlmacenId })
            .HasPrincipalKey(u => new { u.AlmacenId, u.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AjusteInventarioDetalles_Ubicacion_MismoAlmacen_N14");
    }
}
