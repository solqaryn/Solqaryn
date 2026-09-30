using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class CostoEstandarInventarioConfiguration : IEntityTypeConfiguration<CostoEstandarInventario>
{
    public void Configure(EntityTypeBuilder<CostoEstandarInventario> builder)
    {
        builder.ToTable("CostosEstandarInventario", t =>
        {
            t.HasCheckConstraint("CK_CostosEstandar_Costo", "`CostoUnitario` >= 0");
            t.HasCheckConstraint("CK_CostosEstandar_Vigencia", "`VigenteHastaUtc` IS NULL OR `VigenteHastaUtc` > `VigenteDesdeUtc`");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductoVarianteId).IsRequired();
        builder.Property(x => x.CostoUnitario).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.VigenteDesdeUtc).IsRequired();
        builder.Property(x => x.VigenteHastaUtc);
        builder.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<int?>("ProductoVarianteVigenteId")
            .HasComputedColumnSql("CASE WHEN `VigenteHastaUtc` IS NULL THEN `ProductoVarianteId` ELSE NULL END", stored: true);

        builder.HasAlternateKey(x => new { x.ProductoVarianteId, x.Id })
            .HasName("AK_CostosEstandar_Variante_Id");
        builder.HasIndex("ProductoVarianteVigenteId")
            .IsUnique()
            .HasDatabaseName("UX_CostosEstandar_Variante_Vigente");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.VigenteDesdeUtc })
            .HasDatabaseName("IX_CostosEstandar_Variante_Vigencia");

        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CostosEstandar_ProductoVariantes");
    }
}
