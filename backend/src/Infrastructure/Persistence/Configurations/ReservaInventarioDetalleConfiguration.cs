using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia de líneas de reserva por clave física autoritativa:
/// ProductoVariante + Almacén + Ubicación normalizada.
/// </summary>
public sealed class ReservaInventarioDetalleConfiguration : IEntityTypeConfiguration<ReservaInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<ReservaInventarioDetalle> builder)
    {
        builder.ToTable("ReservaInventarioDetalles");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CantidadReservada).IsRequired();
        builder.Property(x => x.CantidadConsumida).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<int>("UbicacionNormalizada")
            .HasComputedColumnSql("COALESCE(`UbicacionAlmacenId`, 0)", stored: true);

        // Índice histórico explícito: el esquema ya lo posee y sigue siendo útil
        // para navegación directa por cabecera de reserva.
        builder.HasIndex(x => x.ReservaInventarioId)
            .HasDatabaseName("IX_ReservaInventarioDetalles_ReservaInventarioId");

        builder.HasIndex(new[]
            {
                nameof(ReservaInventarioDetalle.ReservaInventarioId),
                nameof(ReservaInventarioDetalle.ProductoVarianteId),
                nameof(ReservaInventarioDetalle.AlmacenId),
                "UbicacionNormalizada"
            })
            .IsUnique()
            .HasDatabaseName("UX_ReservaDetalles_ClaveFisica");

        builder.HasIndex(x => new { x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId })
            .HasDatabaseName("IX_ReservaDetalles_ExistenciaFisica");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReservaDetalles_ProductoVariantes_ProductoVarianteId");

        builder.HasOne(x => x.Almacen)
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReservaDetalles_Almacenes_AlmacenId");

        builder.HasOne(x => x.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(x => new { x.AlmacenId, x.UbicacionAlmacenId })
            .HasPrincipalKey(x => new { x.AlmacenId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReservaDetalles_Ubicacion_MismoAlmacen");
    }
}
