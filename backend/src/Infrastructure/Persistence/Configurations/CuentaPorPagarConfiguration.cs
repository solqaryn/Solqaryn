using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de la obligación de cuentas por pagar ERP-N2.8.
/// Una factura de proveedor genera como máximo una cuenta por pagar.
/// </summary>
public sealed class CuentaPorPagarConfiguration : IEntityTypeConfiguration<CuentaPorPagar>
{
    public void Configure(EntityTypeBuilder<CuentaPorPagar> builder)
    {
        builder.ToTable("CuentasPorPagar", table =>
        {
            table.HasCheckConstraint("CK_CuentasPorPagar_IdsValidos",
                "FacturaProveedorId > 0 AND ProveedorId > 0");
            table.HasCheckConstraint("CK_CuentasPorPagar_CondicionPagoValida",
                "CondicionPago IN (1, 2)");
            table.HasCheckConstraint("CK_CuentasPorPagar_EstadoValido",
                "Estado IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_CuentasPorPagar_MonedaIso3",
                "CHAR_LENGTH(TRIM(Moneda)) = 3");
            table.HasCheckConstraint("CK_CuentasPorPagar_MontoOriginalPositivo",
                "MontoOriginal > 0");
            table.HasCheckConstraint("CK_CuentasPorPagar_FechasValidas",
                "FechaVencimientoUtc >= FechaEmisionUtc");
            table.HasCheckConstraint("CK_CuentasPorPagar_ContadoVenceEmision",
                "CondicionPago <> 1 OR FechaVencimientoUtc = FechaEmisionUtc");
            table.HasCheckConstraint("CK_CuentasPorPagar_CreditoVenceDespues",
                "CondicionPago <> 2 OR FechaVencimientoUtc > FechaEmisionUtc");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Moneda).HasMaxLength(3).IsRequired();
        builder.Property(x => x.CondicionPago).HasConversion<int>().IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.MontoOriginal).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Ignore(x => x.MontoAplicado);
        builder.Ignore(x => x.Saldo);

        builder.HasIndex(x => x.FacturaProveedorId)
            .IsUnique()
            .HasDatabaseName("UX_CuentasPorPagar_FacturaProveedorId");

        builder.HasIndex(x => new { x.ProveedorId, x.Estado, x.FechaVencimientoUtc })
            .HasDatabaseName("IX_CuentasPorPagar_Proveedor_Estado_Vencimiento");

        builder.HasIndex(x => new { x.Estado, x.FechaVencimientoUtc })
            .HasDatabaseName("IX_CuentasPorPagar_Estado_Vencimiento");

        builder.HasOne<FacturaProveedor>()
            .WithMany()
            .HasForeignKey(x => x.FacturaProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CuentasPorPagar_FacturasProveedor_FacturaProveedorId");

        builder.HasOne<Proveedor>()
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CuentasPorPagar_Proveedores_ProveedorId");

        builder.HasMany(x => x.Aplicaciones)
            .WithOne()
            .HasForeignKey(x => x.CuentaPorPagarId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AplicacionesCuentaPorPagar_CuentasPorPagar_CuentaPorPagarId");
    }
}
