using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N7.5.C — persistencia tenant-bound para entregas de webhooks entrantes.
/// La unicidad Empresa + Proveedor + EventoExternoId implementa la barrera
/// durable contra replay sin persistir firmas, secretos ni el payload original.
/// </summary>
public sealed class WebhookEntranteConfiguration : IEntityTypeConfiguration<WebhookEntrante>
{
    public void Configure(EntityTypeBuilder<WebhookEntrante> builder)
    {
        builder.ToTable("WebhooksEntrantes", table =>
        {
            table.HasCheckConstraint(
                "CK_WebhooksEntrantes_EmpresaId_Positivo",
                "`EmpresaId` > 0");
            table.HasCheckConstraint(
                "CK_WebhooksEntrantes_Proveedor_NoVacio",
                "CHAR_LENGTH(TRIM(`Proveedor`)) > 0");
            table.HasCheckConstraint(
                "CK_WebhooksEntrantes_EventoExternoId_NoVacio",
                "CHAR_LENGTH(TRIM(`EventoExternoId`)) > 0");
            table.HasCheckConstraint(
                "CK_WebhooksEntrantes_TipoEvento_NoVacio",
                "CHAR_LENGTH(TRIM(`TipoEvento`)) > 0");
            table.HasCheckConstraint(
                "CK_WebhooksEntrantes_PayloadHash_NoVacio",
                "CHAR_LENGTH(TRIM(`PayloadHash`)) > 0");
            table.HasCheckConstraint(
                "CK_WebhooksEntrantes_Estado_Valido",
                "`Estado` BETWEEN 1 AND 4");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmpresaId).IsRequired();

        builder.Property(x => x.Proveedor)
            .IsRequired()
            .HasMaxLength(WebhookEntrante.LongitudMaximaProveedor);

        builder.Property(x => x.EventoExternoId)
            .IsRequired()
            .HasMaxLength(WebhookEntrante.LongitudMaximaEventoExternoId);

        builder.Property(x => x.TipoEvento)
            .IsRequired()
            .HasMaxLength(WebhookEntrante.LongitudMaximaTipoEvento);

        builder.Property(x => x.PayloadHash)
            .IsRequired()
            .HasMaxLength(WebhookEntrante.LongitudMaximaPayloadHash);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(WebhookEntrante.LongitudMaximaCorrelationId);

        builder.Property(x => x.EmitidoEnUtc).IsRequired();
        builder.Property(x => x.RecibidoEnUtc).IsRequired();

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(EstadoWebhookEntrante.Recibido);

        builder.Property(x => x.ProcesandoDesdeUtc);
        builder.Property(x => x.ProcesadoEnUtc);
        builder.Property(x => x.RechazadoEnUtc);

        builder.Property(x => x.MotivoRechazo)
            .HasMaxLength(WebhookEntrante.LongitudMaximaMotivoRechazo);

        builder.HasIndex(x => new { x.EmpresaId, x.Proveedor, x.EventoExternoId })
            .IsUnique()
            .HasDatabaseName("UX_WebhooksEntrantes_Empresa_Proveedor_Evento");

        builder.HasIndex(x => new { x.EmpresaId, x.Estado, x.RecibidoEnUtc, x.Id })
            .HasDatabaseName("IX_WebhooksEntrantes_Empresa_Estado_Recibido");

        builder.HasIndex(x => new { x.EmpresaId, x.CorrelationId })
            .HasDatabaseName("IX_WebhooksEntrantes_Empresa_Correlation");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
