using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");
        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Descripcion).HasMaxLength(500);
        builder.Property(c => c.Activa).HasDefaultValue(true);
        builder.Property(c => c.Eliminada).HasDefaultValue(false);
        builder.HasIndex(c => c.Nombre).HasDatabaseName("IX_Categorias_Nombre");
        builder.HasIndex(c => new { c.Eliminada, c.Activa }).HasDatabaseName("IX_Categorias_Estado");
        builder.HasQueryFilter(c => !c.Eliminada);

        builder.HasMany(c => c.Productos)
            .WithOne(p => p.Categoria)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
