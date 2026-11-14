using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de NotaCreditoProveedor para ERP-N2.7.
/// Mantiene la nota como documento financiero del proveedor y no materializa por sí misma
/// movimientos físicos de inventario, Kardex ni aplicación de saldos de CxP.
/// </summary>
public sealed class NotaCreditoProveedorConfiguration : IEntityTypeConfiguration<NotaCreditoProveedor>
{
    public void Configure(EntityTypeBuilder<NotaCreditoProveedor> builder)
    {
        builder.ToTable("NotasCreditoProveedor", table =>
        {
            table.HasCheckConstraint("CK_NotasCreditoProveedor_IdsValidos",
                "ProveedorId > 0 AND FacturaProveedorId > 0 AND (DevolucionProveedorId IS NULL OR DevolucionProveedorId > 0)");
            table.HasCheckConstraint("CK_NotasCreditoProveedor_EstadoValido",
                "Estado IN (1, 2, 3)");
            table.HasCheckConstraint("CK_NotasCreditoProveedor_MonedaIso3",
                "CHAR_LENGTH(TRIM(Moneda)) = 3");
            table.HasCheckConstraint("CK_NotasCreditoProveedor_ImportesNoNegativos",
                "SubtotalCredito >= 0 AND ImpuestoCredito >= 0 AND (SubtotalCredito + ImpuestoCredito) > 0");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NumeroNotaCredito).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ProveedorNombreSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Moneda).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ReferenciaFiscal).HasMaxLength(120);
        builder.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Observaciones).HasMaxLength(1000);
        builder.Property(x => x.SubtotalCredito).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ImpuestoCredito).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.RegistradaPorNombreSnapshot).HasMaxLength(150);
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Ignore(x => x.TotalCredito);
        builder.Ignore(x => x.EsEditable);

        builder.HasIndex(x => new { x.ProveedorId, x.NumeroNotaCredito })
            .IsUnique()
            .HasDatabaseName("UX_NotasCreditoProveedor_Proveedor_Numero");
        builder.HasIndex(x => x.FacturaProveedorId)
            .HasDatabaseName("IX_NotasCreditoProveedor_FacturaProveedorId");
        builder.HasIndex(x => x.DevolucionProveedorId)
            .HasDatabaseName("IX_NotasCreditoProveedor_DevolucionProveedorId");
        builder.HasIndex(x => new { x.ProveedorId, x.Estado })
            .HasDatabaseName("IX_NotasCreditoProveedor_Proveedor_Estado");
        builder.HasIndex(x => x.FechaEmisionUtc)
            .HasDatabaseName("IX_NotasCreditoProveedor_FechaEmisionUtc");

        builder.HasOne<Proveedor>()
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_NotasCreditoProveedor_Proveedores_ProveedorId");

        builder.HasOne<FacturaProveedor>()
            .WithMany()
            .HasForeignKey(x => x.FacturaProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_NotasCreditoProveedor_FacturasProveedor_FacturaProveedorId");

        builder.HasOne<DevolucionProveedor>()
            .WithMany()
            .HasForeignKey(x => x.DevolucionProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_NotasCreditoProveedor_DevolucionesProveedor_DevolucionProveedorId");
    }
}
