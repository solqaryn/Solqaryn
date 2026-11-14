using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de las líneas físicas documentales de recepción ERP-N2.3.
/// Conserva la clave Variante/Almacén/Ubicación sin materializar stock todavía.
/// </summary>
public sealed class RecepcionCompraDetalleConfiguration : IEntityTypeConfiguration<RecepcionCompraDetalle>
{
    public void Configure(EntityTypeBuilder<RecepcionCompraDetalle> builder)
    {
        builder.ToTable("RecepcionCompraDetalles", table =>
        {
            table.HasCheckConstraint("CK_RecepcionCompraDetalles_CantidadesNoNegativas",
                "CantidadRecibida >= 0 AND CantidadDanada >= 0 AND CantidadFaltante >= 0 AND CantidadSobrante >= 0");
            table.HasCheckConstraint("CK_RecepcionCompraDetalles_BalanceFisico",
                "CantidadDanada + CantidadSobrante <= CantidadRecibida");
            table.HasCheckConstraint("CK_RecepcionCompraDetalles_ActividadFisica",
                "CantidadRecibida > 0 OR CantidadFaltante > 0");
            table.HasCheckConstraint("CK_RecepcionCompraDetalles_CostoNoNegativo",
                "CostoUnitarioSnapshot >= 0");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CantidadRecibida).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CantidadDanada).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CantidadFaltante).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CantidadSobrante).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CostoUnitarioSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoNombreSnapshot).HasMaxLength(250);
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Ignore(x => x.CantidadAceptada);
        builder.Ignore(x => x.TieneActividadFisica);

        // MySQL permite varios NULL en índices UNIQUE. La ubicación raíz se normaliza
        // a 0 para impedir dos veces la misma línea/clave física dentro de una recepción.
        builder.Property<int>("UbicacionAlmacenIdUnica")
            .HasComputedColumnSql("IFNULL(UbicacionAlmacenId, 0)", stored: true);

        builder.HasIndex(new[] { "RecepcionCompraId", "OrdenCompraDetalleId", "AlmacenId", "UbicacionAlmacenIdUnica" })
            .IsUnique()
            .HasDatabaseName("UX_RecepcionCompraDetalles_Recepcion_Linea_Almacen_Ubicacion");

        builder.HasIndex(x => x.OrdenCompraDetalleId)
            .HasDatabaseName("IX_RecepcionCompraDetalles_OrdenCompraDetalleId");

        builder.HasIndex(x => new { x.ProductoId, x.ProductoVarianteId })
            .HasDatabaseName("IX_RecepcionCompraDetalles_Producto_Variante");

        builder.HasIndex(x => x.AlmacenId)
            .HasDatabaseName("IX_RecepcionCompraDetalles_AlmacenId");

        builder.HasOne(x => x.OrdenCompraDetalle)
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecepcionCompraDetalles_OrdenCompraDetalles_OrdenCompraDetalleId");

        builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecepcionCompraDetalles_Productos_ProductoId");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecepcionCompraDetalles_ProductoVariantes_ProductoVarianteId");

        builder.HasOne(x => x.Almacen)
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecepcionCompraDetalles_Almacenes_AlmacenId");

        builder.HasOne(x => x.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(x => new { x.AlmacenId, x.UbicacionAlmacenId })
            .HasPrincipalKey(x => new { x.AlmacenId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecepcionCompraDetalles_Ubicacion_MismoAlmacen");
    }
}
