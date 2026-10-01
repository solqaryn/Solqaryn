using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de las líneas documentales de OrdenCompra ERP-N2.2.
/// No materializa recepción, existencias, Kardex ni costeo.
/// </summary>
public sealed class OrdenCompraDetalleConfiguration : IEntityTypeConfiguration<OrdenCompraDetalle>
{
    public void Configure(EntityTypeBuilder<OrdenCompraDetalle> builder)
    {
        builder.ToTable("OrdenCompraDetalles");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CantidadOrdenada).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.PrecioUnitario).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Descuento).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Impuesto).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Observacion).HasMaxLength(500);
        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoNombreSnapshot).HasMaxLength(250);
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.OrdenCompraId)
            .HasDatabaseName("IX_OrdenCompraDetalles_OrdenCompraId");

        builder.HasIndex(x => new { x.ProductoId, x.ProductoVarianteId })
            .HasDatabaseName("IX_OrdenCompraDetalles_Producto_Variante");

        builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_OrdenCompraDetalles_Productos_ProductoId");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_OrdenCompraDetalles_ProductoVariantes_ProductoVarianteId");
    }
}
