using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class LoteInventarioConfiguration : IEntityTypeConfiguration<LoteInventario>
{
    public void Configure(EntityTypeBuilder<LoteInventario> builder)
    {
        builder.ToTable("LotesInventario");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.ProductoVarianteId, x.Id })
            .HasName("AK_LotesInventario_Variante_Id");
        builder.Property(x => x.Codigo).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.ProductoVarianteId, x.Codigo })
            .IsUnique()
            .HasDatabaseName("UX_LotesInventario_Variante_Codigo");
        builder.HasIndex(x => x.FechaVencimiento)
            .HasDatabaseName("IX_LotesInventario_FechaVencimiento");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_LotesInventario_Fechas",
            "`FechaFabricacion` IS NULL OR `FechaVencimiento` IS NULL OR `FechaVencimiento` >= `FechaFabricacion`"));
    }
}
