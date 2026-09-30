using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class PedidoVentaDetalleConfiguration : IEntityTypeConfiguration<PedidoVentaDetalle>
{
    public void Configure(EntityTypeBuilder<PedidoVentaDetalle> builder)
    {
        builder.ToTable("PedidoVentaDetalles", table =>
        {
            table.HasCheckConstraint("CK_PedidoVentaDetalles_Cantidad_Positiva", "`Cantidad` > 0");
            table.HasCheckConstraint("CK_PedidoVentaDetalles_PrecioUnitario_NoNegativo", "`PrecioUnitario` >= 0");
        });

        builder.Property(d => d.Cantidad).HasPrecision(18, 4);
        builder.Property(d => d.PrecioUnitario).HasPrecision(18, 4);
        builder.Property(d => d.ProductoSkuSnapshot).HasMaxLength(80);
        builder.Property(d => d.ProductoNombreSnapshot).HasMaxLength(150);
        builder.Property(d => d.ProductoMarcaSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoModeloSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoTallaSnapshot).HasMaxLength(100);

        builder.HasIndex(d => d.PedidoVentaId)
            .HasDatabaseName("IX_PedidoVentaDetalles_PedidoVentaId");
        builder.HasIndex(d => d.ProductoId)
            .HasDatabaseName("IX_PedidoVentaDetalles_ProductoId");
        builder.HasIndex(d => d.ProductoVarianteId)
            .HasDatabaseName("IX_PedidoVentaDetalles_ProductoVarianteId");

        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ProductoVariante)
            .WithMany()
            .HasForeignKey(d => d.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
