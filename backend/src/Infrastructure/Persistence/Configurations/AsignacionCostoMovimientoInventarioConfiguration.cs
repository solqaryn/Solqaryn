using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class AsignacionCostoMovimientoInventarioConfiguration : IEntityTypeConfiguration<AsignacionCostoMovimientoInventario>
{
    public void Configure(EntityTypeBuilder<AsignacionCostoMovimientoInventario> builder)
    {
        builder.ToTable("AsignacionesCostoMovimientoInventario", t =>
        {
            t.HasCheckConstraint("CK_AsignacionesCosto_Metodo", "`Metodo` IN (1,2,3)");
            t.HasCheckConstraint("CK_AsignacionesCosto_Cantidad", "`Cantidad` > 0");
            t.HasCheckConstraint("CK_AsignacionesCosto_Costo", "`CostoUnitario` >= 0");
            t.HasCheckConstraint("CK_AsignacionesCosto_CapaPorMetodo", "(`Metodo` = 2 AND `CapaCostoInventarioId` IS NOT NULL) OR (`Metodo` <> 2 AND `CapaCostoInventarioId` IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MovimientoInventarioId).IsRequired();
        builder.Property(x => x.ProductoVarianteId).IsRequired();
        builder.Property(x => x.CapaCostoInventarioId);
        builder.Property(x => x.Metodo).HasConversion<int>().IsRequired();
        builder.Property(x => x.Cantidad).IsRequired();
        builder.Property(x => x.CostoUnitario).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.MovimientoInventarioId)
            .HasDatabaseName("IX_AsignacionesCosto_Movimiento");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.CapaCostoInventarioId })
            .HasDatabaseName("IX_AsignacionesCosto_Capa");

        builder.HasOne<MovimientoInventario>()
            .WithMany()
            .HasForeignKey(x => x.MovimientoInventarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AsignacionesCosto_MovimientosInventario");
        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AsignacionesCosto_ProductoVariantes");
        builder.HasOne<CapaCostoInventario>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductoVarianteId, x.CapaCostoInventarioId })
            .HasPrincipalKey(x => new { x.ProductoVarianteId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AsignacionesCosto_Capa_MismaVariante");
    }
}
