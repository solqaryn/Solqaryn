using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class CompraDocumentoConfiguration : IEntityTypeConfiguration<CompraDocumento>
{
    public void Configure(EntityTypeBuilder<CompraDocumento> builder)
    {
        builder.ToTable("CompraDocumentos");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.NombreOriginal).IsRequired().HasMaxLength(255);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Url).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.PublicId).IsRequired().HasMaxLength(500);
        builder.Property(d => d.ResourceType).IsRequired().HasMaxLength(20);
        builder.Property(d => d.Eliminado).HasDefaultValue(false);

        builder.HasIndex(d => new { d.CompraId, d.Eliminado });
        builder.HasQueryFilter(d => !d.Eliminado);

        builder.HasOne(d => d.Compra)
            .WithMany()
            .HasForeignKey(d => d.CompraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
