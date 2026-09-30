using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class EnlacePublicoFacturaConfiguration : IEntityTypeConfiguration<EnlacePublicoFactura>
{
    public void Configure(EntityTypeBuilder<EnlacePublicoFactura> builder)
    {
        builder.ToTable("EnlacesPublicosFactura");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Token).IsRequired().HasMaxLength(64);
        builder.Property(e => e.VecesAccedido).IsConcurrencyToken();
        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.FacturaId);
    }
}

public class HistorialEnvioFacturaConfiguration : IEntityTypeConfiguration<HistorialEnvioFactura>
{
    public void Configure(EntityTypeBuilder<HistorialEnvioFactura> builder)
    {
        builder.ToTable("HistorialEnviosFactura");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Canal).IsRequired().HasMaxLength(20);
        builder.Property(h => h.Destinatario).IsRequired().HasMaxLength(150);
        builder.Property(h => h.Resultado).IsRequired().HasMaxLength(20);
        builder.Property(h => h.Error).HasMaxLength(500);
        builder.HasIndex(h => h.FacturaId);
    }
}
