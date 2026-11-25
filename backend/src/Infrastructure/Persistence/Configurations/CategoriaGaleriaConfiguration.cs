using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class CategoriaGaleriaConfiguration : IEntityTypeConfiguration<CategoriaGaleria>
{
    public void Configure(EntityTypeBuilder<CategoriaGaleria> builder)
    {
        builder.ToTable("CategoriasGalerias");
        builder.HasKey(x => new { x.EmpresaId, x.CategoriaId });
        builder.Property(x => x.Version).IsRequired().HasMaxLength(32).IsConcurrencyToken();
        builder.Property(x => x.ImagenesJson).IsRequired().HasColumnType("longtext");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CategoriaId);
    }
}
