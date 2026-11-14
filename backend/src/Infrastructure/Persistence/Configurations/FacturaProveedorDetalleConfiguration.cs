using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de las líneas documentales de FacturaProveedor ERP-N2.4.
/// No materializa recepción, existencias, Kardex ni movimientos financieros.
/// </summary>
public sealed class FacturaProveedorDetalleConfiguration : IEntityTypeConfiguration<FacturaProveedorDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaProveedorDetalle> builder)
    {
        builder.ToTable("FacturaProveedorDetalles", table =>
        {
            table.HasCheckConstraint("CK_FacturaProveedorDetalles_IdsValidos",
                "OrdenCompraDetalleId > 0 AND ProductoId > 0 AND (ProductoVarianteId IS NULL OR ProductoVarianteId > 0)");
            table.HasCheckConstraint("CK_FacturaProveedorDetalles_ImportesValidos",
                "CantidadFacturada > 0 AND PrecioUnitarioSnapshot >= 0 AND DescuentoSnapshot >= 0 AND ImpuestoSnapshot >= 0");
            table.HasCheckConstraint("CK_FacturaProveedorDetalles_DescuentoValido",
                "DescuentoSnapshot <= CantidadFacturada * PrecioUnitarioSnapshot");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CantidadFacturada).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.PrecioUnitarioSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.DescuentoSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ImpuestoSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoNombreSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.Observacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.FacturaProveedorId)
            .HasDatabaseName("IX_FacturaProveedorDetalles_FacturaProveedorId");

        builder.HasIndex(x => new { x.FacturaProveedorId, x.OrdenCompraDetalleId })
            .IsUnique()
            .HasDatabaseName("UX_FacturaProveedorDetalles_Factura_OrdenDetalle");

        builder.HasIndex(x => new { x.ProductoId, x.ProductoVarianteId })
            .HasDatabaseName("IX_FacturaProveedorDetalles_Producto_Variante");

        builder.HasOne(x => x.OrdenCompraDetalle)
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FacturaProveedorDetalles_OrdenCompraDetalles_OrdenCompraDetalleId");

        builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FacturaProveedorDetalles_Productos_ProductoId");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FacturaProveedorDetalles_ProductoVariantes_ProductoVarianteId");
    }
}
