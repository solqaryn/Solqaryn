using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica del stock vivo por variante y almacén para ERP-N1.4.
/// StockDisponible y la clave NULL-safe de ubicación se derivan físicamente en MySQL.
/// </summary>
public sealed class ExistenciaVarianteConfiguration : IEntityTypeConfiguration<ExistenciaVariante>
{
    public void Configure(EntityTypeBuilder<ExistenciaVariante> builder)
    {
        builder.ToTable("ExistenciasVariante");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ProductoVarianteId).IsRequired();
        builder.Property(x => x.AlmacenId).IsRequired();
        builder.Property(x => x.UbicacionAlmacenId);
        builder.Property(x => x.StockFisico).IsRequired();
        builder.Property(x => x.StockReservado).IsRequired();
        builder.Property(x => x.StockTransito).IsRequired();
        builder.Property(x => x.StockMinimo).IsRequired();
        builder.Property(x => x.StockMaximo);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property(x => x.StockDisponible)
            .HasComputedColumnSql("StockFisico - StockReservado", stored: true);

        // MySQL permite varios NULL en un índice UNIQUE. La columna generada
        // normaliza la ubicación raíz a 0 (IDs reales son autoincrementales > 0)
        // para garantizar una sola existencia raíz por variante+almacén.
        builder.Property<int>("UbicacionAlmacenIdUnica")
            .HasComputedColumnSql("IFNULL(UbicacionAlmacenId, 0)", stored: true);

        builder.HasIndex(new[] { "ProductoVarianteId", "AlmacenId", "UbicacionAlmacenIdUnica" })
            .IsUnique()
            .HasDatabaseName("UX_ExistenciasVariante_Variante_Almacen_Ubicacion");
        builder.HasIndex(x => x.AlmacenId)
            .HasDatabaseName("IX_ExistenciasVariante_AlmacenId");
        builder.HasIndex(x => x.UbicacionAlmacenId)
            .HasDatabaseName("IX_ExistenciasVariante_UbicacionAlmacenId");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.AlmacenId })
            .HasDatabaseName("IX_ExistenciasVariante_Variante_Almacen");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired()
            .HasConstraintName("FK_ExistenciasVariante_ProductoVariantes_ProductoVarianteId");

        builder.HasOne(x => x.Almacen)
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired()
            .HasConstraintName("FK_ExistenciasVariante_Almacenes_AlmacenId");

        // La FK compuesta impide asociar una existencia a una ubicación de otro almacén.
        builder.HasOne(x => x.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(x => new { x.AlmacenId, x.UbicacionAlmacenId })
            .HasPrincipalKey(x => new { x.AlmacenId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ExistenciasVariante_Ubicacion_MismoAlmacen");
    }
}
