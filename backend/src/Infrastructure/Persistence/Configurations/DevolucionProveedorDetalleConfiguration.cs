using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class DevolucionProveedorDetalleConfiguration : IEntityTypeConfiguration<DevolucionProveedorDetalle>
{
    public void Configure(EntityTypeBuilder<DevolucionProveedorDetalle> builder)
    {
        builder.ToTable("DevolucionProveedorDetalles", table =>
        {
            table.HasCheckConstraint("CK_DevolucionProveedorDetalles_IdsValidos",
                "DevolucionProveedorId > 0 AND RecepcionCompraDetalleId > 0 AND OrdenCompraDetalleId > 0 AND ProductoId > 0 AND AlmacenId > 0");
            table.HasCheckConstraint("CK_DevolucionProveedorDetalles_CantidadPositiva",
                "Cantidad > 0");
            table.HasCheckConstraint("CK_DevolucionProveedorDetalles_CostosNoNegativos",
                "CostoUnitarioSnapshot >= 0 AND ImpuestoUnitarioSnapshot >= 0");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Cantidad).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CostoUnitarioSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ImpuestoUnitarioSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ProductoSkuSnapshot).HasMaxLength(120);
        builder.Property(x => x.ProductoNombreSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ProductoMarcaSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoModeloSnapshot).HasMaxLength(150);
        builder.Property(x => x.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(x => x.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.DevolucionProveedorId, x.RecepcionCompraDetalleId })
            .IsUnique()
            .HasDatabaseName("UX_DevolucionProveedorDetalles_Devolucion_RecepcionDetalle");
        builder.HasIndex(x => x.OrdenCompraDetalleId)
            .HasDatabaseName("IX_DevolucionProveedorDetalles_OrdenCompraDetalleId");
        builder.HasIndex(x => x.ProductoId)
            .HasDatabaseName("IX_DevolucionProveedorDetalles_ProductoId");
        builder.HasIndex(x => x.ProductoVarianteId)
            .HasDatabaseName("IX_DevolucionProveedorDetalles_ProductoVarianteId");
        builder.HasIndex(x => x.AlmacenId)
            .HasDatabaseName("IX_DevolucionProveedorDetalles_AlmacenId");
        builder.HasIndex(x => x.UbicacionAlmacenId)
            .HasDatabaseName("IX_DevolucionProveedorDetalles_UbicacionAlmacenId");

        builder.HasOne<RecepcionCompraDetalle>()
            .WithMany()
            .HasForeignKey(x => x.RecepcionCompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionProveedorDetalles_RecepcionCompraDetalles_RecepcionCompraDetalleId");
        builder.HasOne<OrdenCompraDetalle>()
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionProveedorDetalles_OrdenCompraDetalles_OrdenCompraDetalleId");
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionProveedorDetalles_Productos_ProductoId");
        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionProveedorDetalles_ProductoVariantes_ProductoVarianteId");
        builder.HasOne<Almacen>()
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionProveedorDetalles_Almacenes_AlmacenId");
        builder.HasOne<UbicacionAlmacen>()
            .WithMany()
            .HasForeignKey(x => x.UbicacionAlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionProveedorDetalles_UbicacionesAlmacen_UbicacionAlmacenId");
    }
}
