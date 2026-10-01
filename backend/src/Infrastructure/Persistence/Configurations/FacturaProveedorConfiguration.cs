using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de la cabecera de factura de proveedor ERP-N2.4.
/// Mantiene separadas OrdenCompra, RecepcionCompra y FacturaProveedor.
/// </summary>
public sealed class FacturaProveedorConfiguration : IEntityTypeConfiguration<FacturaProveedor>
{
    public void Configure(EntityTypeBuilder<FacturaProveedor> builder)
    {
        builder.ToTable("FacturasProveedor", table =>
        {
            table.HasCheckConstraint("CK_FacturasProveedor_IdsValidos",
                "ProveedorId > 0 AND OrdenCompraId > 0");
            table.HasCheckConstraint("CK_FacturasProveedor_EstadoValido",
                "Estado IN (1, 2, 3)");
            table.HasCheckConstraint("CK_FacturasProveedor_MonedaIso3",
                "CHAR_LENGTH(TRIM(Moneda)) = 3");
            table.HasCheckConstraint("CK_FacturasProveedor_FechasValidas",
                "FechaVencimientoUtc IS NULL OR FechaVencimientoUtc >= FechaEmisionUtc");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NumeroFactura).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.ProveedorNombreSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ProveedorDocumentoSnapshot).HasMaxLength(120);
        builder.Property(x => x.Moneda).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ReferenciaFiscal).HasMaxLength(120);
        builder.Property(x => x.Observaciones).HasMaxLength(1000);
        builder.Property(x => x.RegistradaPorNombreSnapshot).HasMaxLength(150);
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.ProveedorId, x.NumeroFactura })
            .IsUnique()
            .HasDatabaseName("UX_FacturasProveedor_Proveedor_NumeroFactura");

        builder.HasIndex(x => x.OrdenCompraId)
            .HasDatabaseName("IX_FacturasProveedor_OrdenCompraId");

        builder.HasIndex(x => new { x.Estado, x.FechaEmisionUtc })
            .HasDatabaseName("IX_FacturasProveedor_Estado_FechaEmision");

        builder.HasIndex(x => x.FechaVencimientoUtc)
            .HasDatabaseName("IX_FacturasProveedor_FechaVencimiento");

        builder.HasOne(x => x.Proveedor)
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FacturasProveedor_Proveedores_ProveedorId");

        builder.HasOne(x => x.OrdenCompra)
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FacturasProveedor_OrdenesCompra_OrdenCompraId");

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.FacturaProveedor)
            .HasForeignKey(x => x.FacturaProveedorId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_FacturaProveedorDetalles_FacturasProveedor_FacturaProveedorId");
    }
}
