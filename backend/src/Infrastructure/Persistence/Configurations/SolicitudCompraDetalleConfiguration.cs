using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia de las líneas documentales de una solicitud de compra ERP-N2.1.
/// La variante es opcional, pero cuando existe debe pertenecer al producto solicitado.
/// </summary>
public sealed class SolicitudCompraDetalleConfiguration : IEntityTypeConfiguration<SolicitudCompraDetalle>
{
    public void Configure(EntityTypeBuilder<SolicitudCompraDetalle> builder)
    {
        builder.ToTable("SolicitudCompraDetalles");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CantidadSolicitada).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CostoEstimadoUnitario).HasPrecision(18, 4);
        builder.Property(x => x.Observacion).HasMaxLength(500);
        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoNombreSnapshot).HasMaxLength(250);
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.SolicitudCompraId)
            .HasDatabaseName("IX_SolicitudCompraDetalles_SolicitudCompraId");

        builder.HasIndex(x => new { x.ProductoId, x.ProductoVarianteId })
            .HasDatabaseName("IX_SolicitudCompraDetalles_Producto_Variante");

        builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SolicitudCompraDetalles_Productos_ProductoId");

        builder.HasOne(x => x.ProductoVariante)
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SolicitudCompraDetalles_ProductoVariantes_ProductoVarianteId");
    }
}
