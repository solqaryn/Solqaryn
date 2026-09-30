using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ConsumoInsumoConfiguration : IEntityTypeConfiguration<ConsumoInsumo>
{
    public void Configure(EntityTypeBuilder<ConsumoInsumo> builder)
    {
        builder.ToTable("ConsumosInsumos");
        builder.Property(c => c.NumeroConsumo).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => c.NumeroConsumo).IsUnique();
        builder.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.AreaDestino).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Motivo).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Observaciones).HasMaxLength(1000);
        builder.Property(c => c.MotivoAnulacion).HasMaxLength(500);
        builder.Property(c => c.ConfirmadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(c => c.AnuladoPorNombreUsuario).HasMaxLength(150);
        builder.Property(c => c.Eliminado).HasDefaultValue(false);
        builder.HasIndex(c => new { c.Estado, c.FechaConsumo });
        builder.HasIndex(c => c.Eliminado);
        builder.HasQueryFilter(c => !c.Eliminado);

        builder.HasMany(c => c.Detalles)
            .WithOne(d => d.ConsumoInsumo)
            .HasForeignKey(d => d.ConsumoInsumoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
