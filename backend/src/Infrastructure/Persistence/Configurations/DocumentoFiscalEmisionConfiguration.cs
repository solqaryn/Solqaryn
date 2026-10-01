using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N7.10.C — persistencia fiscal provider/jurisdiction-neutral con idempotencia durable.
/// No almacena credenciales, claves de idempotencia en claro ni reglas legales universales.
/// </summary>
public sealed class DocumentoFiscalEmisionConfiguration : IEntityTypeConfiguration<DocumentoFiscalEmision>
{
    public void Configure(EntityTypeBuilder<DocumentoFiscalEmision> builder)
    {
        builder.ToTable("DocumentosFiscalesEmision", table =>
        {
            table.HasCheckConstraint("CK_DocFiscal_EmpresaId_Positivo", "`EmpresaId` > 0");
            table.HasCheckConstraint("CK_DocFiscal_FacturaId_Positivo", "`FacturaId` > 0");
            table.HasCheckConstraint("CK_DocFiscal_SucursalId_Positivo", "`SucursalId` IS NULL OR `SucursalId` > 0");
            table.HasCheckConstraint("CK_DocFiscal_Jurisdiccion_NoVacia", "CHAR_LENGTH(TRIM(`Jurisdiccion`)) > 0");
            table.HasCheckConstraint("CK_DocFiscal_Proveedor_NoVacio", "CHAR_LENGTH(TRIM(`Proveedor`)) > 0");
            table.HasCheckConstraint("CK_DocFiscal_TipoDocumento_NoVacio", "CHAR_LENGTH(TRIM(`TipoDocumento`)) > 0");
            table.HasCheckConstraint("CK_DocFiscal_IdempotenciaHash_SHA256", "CHAR_LENGTH(`ClaveIdempotenciaHash`) = 64");
            table.HasCheckConstraint("CK_DocFiscal_HashSnapshot_NoVacio", "CHAR_LENGTH(TRIM(`HashSnapshot`)) > 0");
            table.HasCheckConstraint("CK_DocFiscal_Estado_Valido", "`Estado` BETWEEN 1 AND 3");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmpresaId).IsRequired();
        builder.Property(x => x.SucursalId);
        builder.Property(x => x.FacturaId).IsRequired();
        builder.Property(x => x.Jurisdiccion).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Proveedor).IsRequired().HasMaxLength(80);
        builder.Property(x => x.TipoDocumento).IsRequired().HasMaxLength(80);
        builder.Property(x => x.ClaveIdempotenciaHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.HashSnapshot).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Estado).IsRequired().HasConversion<int>();
        builder.Property(x => x.ReferenciaExterna).HasMaxLength(200);
        builder.Property(x => x.CodigoProveedor).HasMaxLength(120);
        builder.Property(x => x.CreadoUtc).IsRequired();
        builder.Property(x => x.UltimaActualizacionUtc).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(200);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(200);

        builder.HasIndex(x => new { x.EmpresaId, x.Proveedor, x.ClaveIdempotenciaHash })
            .IsUnique()
            .HasDatabaseName("UX_DocFiscal_Empresa_Proveedor_Idempotencia");

        builder.HasIndex(x => new { x.EmpresaId, x.FacturaId, x.CreadoUtc })
            .HasDatabaseName("IX_DocFiscal_Empresa_Factura_Creado");

        builder.HasIndex(x => new { x.EmpresaId, x.Estado, x.UltimaActualizacionUtc, x.Id })
            .HasDatabaseName("IX_DocFiscal_Empresa_Estado_Actualizado");

        builder.HasIndex(x => new { x.EmpresaId, x.Proveedor, x.ReferenciaExterna })
            .IsUnique()
            .HasDatabaseName("UX_DocFiscal_Empresa_Proveedor_Referencia");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Factura)
            .WithMany()
            .HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
