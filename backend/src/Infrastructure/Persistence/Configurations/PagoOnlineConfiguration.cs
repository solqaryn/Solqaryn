using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N7.8.C/D — persistencia tenant-bound para pagos iniciados mediante proveedores externos.
/// Guarda referencias opacas del proveedor e idempotencia durable; nunca PAN, CVV ni
/// credenciales del proveedor.
/// </summary>
public sealed class PagoOnlineConfiguration : IEntityTypeConfiguration<PagoOnline>
{
    public void Configure(EntityTypeBuilder<PagoOnline> builder)
    {
        builder.ToTable("PagosOnline", table =>
        {
            table.HasCheckConstraint("CK_PagosOnline_EmpresaId_Positivo", "`EmpresaId` > 0");
            table.HasCheckConstraint("CK_PagosOnline_FacturaId_Positivo", "`FacturaId` > 0");
            table.HasCheckConstraint("CK_PagosOnline_Proveedor_NoVacio", "CHAR_LENGTH(TRIM(`Proveedor`)) > 0");
            table.HasCheckConstraint("CK_PagosOnline_Monto_Positivo", "`Monto` > 0");
            table.HasCheckConstraint("CK_PagosOnline_Moneda_Valida", "CHAR_LENGTH(TRIM(`Moneda`)) = 3");
            table.HasCheckConstraint("CK_PagosOnline_Estado_Valido", "`Estado` BETWEEN 1 AND 3");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmpresaId).IsRequired();
        builder.Property(x => x.FacturaId).IsRequired();

        builder.Property(x => x.Proveedor)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.ReferenciaProveedor)
            .HasMaxLength(160);

        builder.Property(x => x.ProviderEventId)
            .HasMaxLength(200);

        builder.Property(x => x.ClaveIdempotenciaHash)
            .HasMaxLength(64);

        builder.Property(x => x.Monto)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Moneda)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.UrlPago)
            .HasMaxLength(2048);

        builder.Property(x => x.CreadoUtc).IsRequired();
        builder.Property(x => x.ConfirmadoUtc);

        builder.Property(x => x.UltimoError)
            .HasMaxLength(2000);

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(200);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(200);

        builder.HasIndex(x => new { x.EmpresaId, x.FacturaId, x.CreadoUtc })
            .HasDatabaseName("IX_PagosOnline_Empresa_Factura_Creado");

        builder.HasIndex(x => new { x.EmpresaId, x.Estado, x.CreadoUtc, x.Id })
            .HasDatabaseName("IX_PagosOnline_Empresa_Estado_Creado");

        builder.HasIndex(x => new { x.EmpresaId, x.Proveedor, x.ReferenciaProveedor })
            .IsUnique()
            .HasDatabaseName("UX_PagosOnline_Empresa_Proveedor_Referencia");

        builder.HasIndex(x => new { x.EmpresaId, x.Proveedor, x.ProviderEventId })
            .IsUnique()
            .HasDatabaseName("UX_PagosOnline_Empresa_Proveedor_Evento");

        builder.HasIndex(x => new { x.EmpresaId, x.Proveedor, x.ClaveIdempotenciaHash })
            .IsUnique()
            .HasDatabaseName("UX_PagosOnline_Empresa_Proveedor_Idempotencia");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Factura)
            .WithMany()
            .HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
