using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de DevolucionProveedor para ERP-N2.6.
/// Mantiene la devolución desacoplada de sus efectos posteriores de inventario/CxP.
/// </summary>
public sealed class DevolucionProveedorConfiguration : IEntityTypeConfiguration<DevolucionProveedor>
{
    public void Configure(EntityTypeBuilder<DevolucionProveedor> builder)
    {
        builder.ToTable("DevolucionesProveedor", table =>
        {
            table.HasCheckConstraint("CK_DevolucionesProveedor_IdsValidos",
                "ProveedorId > 0 AND OrdenCompraId > 0 AND RecepcionCompraId > 0 AND FacturaProveedorId > 0");
            table.HasCheckConstraint("CK_DevolucionesProveedor_EstadoValido",
                "Estado IN (1, 2, 3)");
            table.HasCheckConstraint("CK_DevolucionesProveedor_MonedaIso3",
                "CHAR_LENGTH(TRIM(Moneda)) = 3");
            table.HasCheckConstraint("CK_DevolucionesProveedor_IdempotenciaAtomica",
                "(IdempotencyKey IS NULL AND IdempotencyFingerprint IS NULL) OR (IdempotencyKey IS NOT NULL AND IdempotencyFingerprint IS NOT NULL)");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NumeroDevolucion).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ProveedorNombreSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Moneda).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Observaciones).HasMaxLength(1000);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.IdempotencyFingerprint).HasMaxLength(64);
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.ConfirmadaPorNombreSnapshot).HasMaxLength(150);
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.NumeroDevolucion)
            .IsUnique()
            .HasDatabaseName("UX_DevolucionesProveedor_NumeroDevolucion");
        builder.HasIndex(x => new { x.ProveedorId, x.Estado })
            .HasDatabaseName("IX_DevolucionesProveedor_Proveedor_Estado");
        builder.HasIndex(x => x.OrdenCompraId)
            .HasDatabaseName("IX_DevolucionesProveedor_OrdenCompraId");
        builder.HasIndex(x => x.RecepcionCompraId)
            .HasDatabaseName("IX_DevolucionesProveedor_RecepcionCompraId");
        builder.HasIndex(x => x.FacturaProveedorId)
            .HasDatabaseName("IX_DevolucionesProveedor_FacturaProveedorId");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_DevolucionesProveedor_IdempotencyKey");

        builder.HasOne<Proveedor>()
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionesProveedor_Proveedores_ProveedorId");
        builder.HasOne<OrdenCompra>()
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionesProveedor_OrdenesCompra_OrdenCompraId");
        builder.HasOne<RecepcionCompra>()
            .WithMany()
            .HasForeignKey(x => x.RecepcionCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionesProveedor_RecepcionesCompra_RecepcionCompraId");
        builder.HasOne<FacturaProveedor>()
            .WithMany()
            .HasForeignKey(x => x.FacturaProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_DevolucionesProveedor_FacturasProveedor_FacturaProveedorId");

        builder.HasMany(x => x.Detalles)
            .WithOne()
            .HasForeignKey(x => x.DevolucionProveedorId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_DevolucionProveedorDetalles_DevolucionesProveedor_DevolucionProveedorId");
    }
}
