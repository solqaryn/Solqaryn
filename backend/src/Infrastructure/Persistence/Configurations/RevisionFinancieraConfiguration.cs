using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class RevisionFinancieraConfiguration : IEntityTypeConfiguration<RevisionFinanciera>
{
    public void Configure(EntityTypeBuilder<RevisionFinanciera> builder)
    {
        builder.ToTable("RevisionesFinancieras");
        builder.Property(r => r.RevisadoPorNombreUsuario).IsRequired().HasMaxLength(150);
        builder.Property(r => r.EstadoRevision).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Observaciones).HasMaxLength(1000);
    }
}
