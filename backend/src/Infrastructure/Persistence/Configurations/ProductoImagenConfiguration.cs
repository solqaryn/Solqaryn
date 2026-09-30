using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ProductoImagenConfiguration : IEntityTypeConfiguration<ProductoImagen>
{
    public void Configure(EntityTypeBuilder<ProductoImagen> builder)
    {
        builder.ToTable("ProductoImagenes");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(1000);
        builder.Property(i => i.PublicId).IsRequired().HasMaxLength(500);
        builder.Property(i => i.CreadoPorNombreUsuario).HasMaxLength(100);

        builder.HasOne(i => i.Producto)
            .WithMany(p => p.Imagenes)
            .HasForeignKey(i => i.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ProductoVariante)
            .WithMany(v => v.Imagenes)
            .HasForeignKey(i => i.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);

        // VIRTUAL evita una reconstrucción física de ProductoImagenes en MySQL
        // 8.4. La columna continúa siendo indexable y permite aplicar una
        // restricción UNIQUE robusta aun cuando ProductoVarianteId sea NULL.
        builder.Property<string>("PrincipalAmbitoKey")
            .HasMaxLength(80)
            .HasComputedColumnSql("IF(EsPrincipal = 1, CONCAT(ProductoId, ':', IFNULL(ProductoVarianteId, 0)), NULL)", stored: false);

        builder.HasIndex("PrincipalAmbitoKey")
            .IsUnique()
            .HasDatabaseName("UX_ProductoImagenes_Principal_Ambito");
        builder.HasIndex(i => i.ProductoId)
            .HasDatabaseName("IX_ProductoImagenes_ProductoId");
        builder.HasIndex(i => new { i.ProductoId, i.ProductoVarianteId, i.Orden })
            .HasDatabaseName("IX_ProductoImagenes_Producto_Variante_Orden");
    }
}
