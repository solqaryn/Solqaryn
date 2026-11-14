using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class CargaMasivaErrorConfiguration : IEntityTypeConfiguration<CargaMasivaError>
{
    public void Configure(EntityTypeBuilder<CargaMasivaError> builder)
    {
        builder.ToTable("CargaMasivaErrores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Campo).HasMaxLength(120);
        builder.Property(x => x.Codigo).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Mensaje).HasMaxLength(700).IsRequired();
        builder.Property(x => x.ValorOriginal).HasMaxLength(1000);
        builder.HasIndex(x => new { x.CargaMasivaId, x.NumeroFila });
    }
}
