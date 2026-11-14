using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class SerieInventarioConfiguration : IEntityTypeConfiguration<SerieInventario>
{
    public void Configure(EntityTypeBuilder<SerieInventario> builder)
    {
        builder.ToTable("SeriesInventario");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NumeroSerie).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.NumeroSerie)
            .IsUnique()
            .HasDatabaseName("UX_SeriesInventario_NumeroSerie");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.Estado })
            .HasDatabaseName("IX_SeriesInventario_Variante_Estado");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.LoteInventarioId })
            .HasDatabaseName("IX_SeriesInventario_Variante_LoteInventarioId");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LoteInventario)
            .WithMany()
            .HasForeignKey(x => new { x.ProductoVarianteId, x.LoteInventarioId })
            .HasPrincipalKey(x => new { x.ProductoVarianteId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SeriesInventario_LotesInventario_Variante_Lote");
    }
}
