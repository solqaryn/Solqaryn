using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ProductoVarianteConfiguration : IEntityTypeConfiguration<ProductoVariante>
{
    public void Configure(EntityTypeBuilder<ProductoVariante> builder)
    {
        builder.ToTable("ProductoVariantes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(80);
        builder.Property(x => x.CodigoBarras).HasMaxLength(120);
        builder.Property(x => x.Costo).HasPrecision(18, 2);
        builder.Property(x => x.Precio).HasPrecision(18, 2);
        builder.Property(x => x.EsTecnica).HasDefaultValue(false);
        builder.Property(x => x.ControlaLote).HasDefaultValue(false);
        builder.Property(x => x.ControlaNumeroSerie).HasDefaultValue(false);
        builder.Property(x => x.ControlaFechaVencimiento).HasDefaultValue(false);
        builder.Property(x => x.DiasAlertaVencimiento);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ProductoVariantes_TrazabilidadVencimiento",
            "`ControlaFechaVencimiento` = 0 OR `ControlaLote` = 1"));
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ProductoVariantes_AlertaVencimiento",
            "(`DiasAlertaVencimiento` IS NULL AND `ControlaFechaVencimiento` = 0) OR (`ControlaFechaVencimiento` = 1 AND (`DiasAlertaVencimiento` IS NULL OR `DiasAlertaVencimiento` >= 0))"));

        builder.Property<string?>("IdentidadActivaUnica")
            .HasMaxLength(160)
            .HasComputedColumnSql(
                "CASE WHEN `Eliminado` = 0 THEN CONCAT(`ProductoId`, ':', COALESCE(`MarcaId`, 0), ':', COALESCE(`ModeloId`, 0), ':', COALESCE(`ColorId`, 0), ':', COALESCE(`TallaId`, 0)) ELSE NULL END",
                stored: true);

        builder.Property<int?>("ProductoTecnicoUnico")
            .HasComputedColumnSql(
                "CASE WHEN `EsTecnica` = 1 AND `Eliminado` = 0 THEN `ProductoId` ELSE NULL END",
                stored: true);

        builder.HasIndex("IdentidadActivaUnica")
            .IsUnique()
            .HasDatabaseName("UX_ProductoVariantes_IdentidadActiva");
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasIndex(x => x.CodigoBarras).IsUnique();
        builder.HasIndex("ProductoTecnicoUnico")
            .IsUnique()
            .HasDatabaseName("IX_ProductoVariantes_ProductoTecnicoUnico");
        builder.HasIndex(x => new { x.ProductoId, x.MarcaId, x.ModeloId, x.ColorId, x.TallaId })
            .HasDatabaseName("IX_ProductoVariantes_Dimensiones");

        builder.HasOne(x => x.Producto)
            .WithMany(x => x.Variantes)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Marca)
            .WithMany()
            .HasForeignKey(x => x.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Modelo)
            .WithMany()
            .HasForeignKey(x => x.ModeloId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Color)
            .WithMany()
            .HasForeignKey(x => x.ColorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Talla)
            .WithMany()
            .HasForeignKey(x => x.TallaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
