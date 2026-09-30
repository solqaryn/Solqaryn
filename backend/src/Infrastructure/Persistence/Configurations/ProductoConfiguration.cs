using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");

        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Marca).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Modelo).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Descripcion).HasMaxLength(1000);
        builder.Property(p => p.TipoInventario)
            .HasConversion<int>()
            .HasDefaultValue(TipoInventario.MercaderiaVenta);
        builder.Property(p => p.Costo).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Precio).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Activo).HasDefaultValue(true);
        builder.Property(p => p.EsDestacado).HasDefaultValue(false);
        builder.Property(p => p.Eliminado).HasDefaultValue(false);

        builder.HasQueryFilter(p => !p.Eliminado);

        builder.Ignore(p => p.ImagenPrincipal);
        builder.Ignore(p => p.TieneStockBajo);
        builder.Ignore(p => p.EstaAgotado);
        builder.Ignore(p => p.Color);
        builder.Ignore(p => p.Talla);

        builder.HasIndex(p => p.Nombre).HasDatabaseName("IX_Productos_Nombre");
        builder.HasIndex(p => p.Marca).HasDatabaseName("IX_Productos_Marca");
        builder.HasIndex(p => p.Modelo).HasDatabaseName("IX_Productos_Modelo");
        builder.HasIndex(p => p.ColorId).HasDatabaseName("IX_Productos_ColorId");
        builder.HasIndex(p => p.TallaId).HasDatabaseName("IX_Productos_TallaId");
        builder.HasIndex(p => p.MarcaId).HasDatabaseName("IX_Productos_MarcaId");
        builder.HasIndex(p => p.ModeloId).HasDatabaseName("IX_Productos_ModeloId");
        builder.HasIndex(p => new { p.EsDestacado, p.Activo }).HasDatabaseName("IX_Productos_EsDestacado_Activo");
        builder.HasIndex(p => new { p.Eliminado, p.Activo }).HasDatabaseName("IX_Productos_Estado");
        builder.HasIndex(p => new { p.TipoInventario, p.Eliminado, p.Activo }).HasDatabaseName("IX_Productos_TipoInventario_Estado");

        // ERP-N0.2: las proyecciones familiares dejan de depender de
        // CatalogosProducto y apuntan al mismo maestro normalizado que las variantes.
        builder.HasOne(p => p.ColorCatalogo)
            .WithMany()
            .HasForeignKey(p => p.ColorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.TallaCatalogo)
            .WithMany()
            .HasForeignKey(p => p.TallaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.MarcaCatalogo)
            .WithMany()
            .HasForeignKey(p => p.MarcaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.ModeloCatalogo)
            .WithMany()
            .HasForeignKey(p => p.ModeloId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
