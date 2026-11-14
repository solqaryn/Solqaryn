using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class VariacionCostoEstandarInventarioConfiguration : IEntityTypeConfiguration<VariacionCostoEstandarInventario>
{
    public void Configure(EntityTypeBuilder<VariacionCostoEstandarInventario> builder)
    {
        builder.ToTable("VariacionesCostoEstandarInventario", t =>
        {
            t.HasCheckConstraint("CK_VariacionesCostoEstandar_Cantidad", "`Cantidad` > 0");
            t.HasCheckConstraint("CK_VariacionesCostoEstandar_Costos", "`CostoRealUnitario` >= 0 AND `CostoEstandarUnitario` >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MovimientoInventarioId).IsRequired();
        builder.Property(x => x.ProductoVarianteId).IsRequired();
        builder.Property(x => x.CostoEstandarInventarioId).IsRequired();
        builder.Property(x => x.Cantidad).IsRequired();
        builder.Property(x => x.CostoRealUnitario).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CostoEstandarUnitario).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.VariacionTotal).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.MovimientoInventarioId)
            .HasDatabaseName("IX_VariacionesCostoEstandar_Movimiento");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.CostoEstandarInventarioId })
            .HasDatabaseName("IX_VariacionesCostoEstandar_Version");

        builder.HasOne<MovimientoInventario>()
            .WithMany()
            .HasForeignKey(x => x.MovimientoInventarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VariacionesCostoEstandar_MovimientosInventario");
        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VariacionesCostoEstandar_ProductoVariantes");
        builder.HasOne<CostoEstandarInventario>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductoVarianteId, x.CostoEstandarInventarioId })
            .HasPrincipalKey(x => new { x.ProductoVarianteId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VariacionesCostoEstandar_Version_MismaVariante");
    }
}
