using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia de líneas físicas de conteo. La clave operativa es
/// Variante + Almacén + Ubicación normalizada dentro del mismo documento.
/// </summary>
public sealed class ConteoInventarioDetalleConfiguration : IEntityTypeConfiguration<ConteoInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<ConteoInventarioDetalle> builder)
    {
        builder.ToTable("ConteoInventarioDetalles");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.StockEsperadoSnapshot).IsRequired();
        builder.Property(x => x.SnapshotMaterializado).HasDefaultValue(false);
        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<int>("UbicacionNormalizada")
            .HasComputedColumnSql("COALESCE(`UbicacionAlmacenId`, 0)", stored: true);

        builder.HasIndex(new[]
            {
                nameof(ConteoInventarioDetalle.ConteoInventarioId),
                nameof(ConteoInventarioDetalle.ProductoVarianteId),
                nameof(ConteoInventarioDetalle.AlmacenId),
                "UbicacionNormalizada"
            })
            .IsUnique()
            .HasDatabaseName("UX_ConteoDetalles_ClaveFisica");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId })
            .HasDatabaseName("IX_ConteoDetalles_ExistenciaFisica");
        builder.HasIndex(x => x.AjusteInventarioId)
            .HasDatabaseName("IX_ConteoDetalles_AjusteInventarioId");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConteoDetalles_ProductoVariantes_ProductoVarianteId");

        builder.HasOne(x => x.Almacen)
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConteoDetalles_Almacenes_AlmacenId");

        builder.HasOne(x => x.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(x => new { x.AlmacenId, x.UbicacionAlmacenId })
            .HasPrincipalKey(x => new { x.AlmacenId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConteoDetalles_Ubicacion_MismoAlmacen");

        builder.HasOne(x => x.AjusteInventario)
            .WithMany()
            .HasForeignKey(x => x.AjusteInventarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConteoDetalles_AjustesInventario_AjusteInventarioId");
    }
}
