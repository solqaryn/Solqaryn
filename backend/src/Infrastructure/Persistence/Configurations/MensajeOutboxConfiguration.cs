using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N7.1.C — almacenamiento durable del outbox. La unicidad tenant + clave de
/// idempotencia impide registrar dos intenciones equivalentes y los índices de
/// claim evitan scans al seleccionar trabajo disponible.
/// </summary>
public sealed class MensajeOutboxConfiguration : IEntityTypeConfiguration<MensajeOutbox>
{
    public void Configure(EntityTypeBuilder<MensajeOutbox> builder)
    {
        builder.ToTable("MensajesOutbox", table =>
        {
            table.HasCheckConstraint(
                "CK_MensajesOutbox_EmpresaId_Positivo",
                "`EmpresaId` > 0");
            table.HasCheckConstraint(
                "CK_MensajesOutbox_Intentos_NoNegativo",
                "`Intentos` >= 0");
            table.HasCheckConstraint(
                "CK_MensajesOutbox_TipoEvento_NoVacio",
                "CHAR_LENGTH(TRIM(`TipoEvento`)) > 0");
            table.HasCheckConstraint(
                "CK_MensajesOutbox_ClaveIdempotencia_NoVacia",
                "CHAR_LENGTH(TRIM(`ClaveIdempotencia`)) > 0");
            table.HasCheckConstraint(
                "CK_MensajesOutbox_Payload_NoVacio",
                "CHAR_LENGTH(TRIM(`PayloadJson`)) > 0");
            table.HasCheckConstraint(
                "CK_MensajesOutbox_Estado_Valido",
                "`Estado` BETWEEN 0 AND 4");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventoId)
            .IsRequired()
            .HasColumnType("char(36)");

        builder.Property(x => x.EmpresaId).IsRequired();

        builder.Property(x => x.TipoEvento)
            .IsRequired()
            .HasMaxLength(MensajeOutbox.LongitudMaximaTipoEvento);

        builder.Property(x => x.PayloadJson)
            .IsRequired()
            .HasColumnType("longtext");

        builder.Property(x => x.ClaveIdempotencia)
            .IsRequired()
            .HasMaxLength(MensajeOutbox.LongitudMaximaClaveIdempotencia);

        builder.Property(x => x.TipoAgregado)
            .HasMaxLength(MensajeOutbox.LongitudMaximaTipoAgregado);

        builder.Property(x => x.IdAgregado)
            .HasMaxLength(MensajeOutbox.LongitudMaximaIdAgregado);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(MensajeOutbox.LongitudMaximaCorrelationId);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(EstadoMensajeOutbox.Pendiente);

        builder.Property(x => x.Intentos)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CreadoEnUtc).IsRequired();
        builder.Property(x => x.DisponibleDesdeUtc).IsRequired();
        builder.Property(x => x.ProcesandoDesdeUtc);
        builder.Property(x => x.EntregadoEnUtc);
        builder.Property(x => x.UltimoIntentoEnUtc);

        builder.Property(x => x.UltimoError)
            .HasMaxLength(MensajeOutbox.LongitudMaximaError);

        builder.HasIndex(x => x.EventoId)
            .IsUnique()
            .HasDatabaseName("UX_MensajesOutbox_EventoId");

        builder.HasIndex(x => new { x.EmpresaId, x.ClaveIdempotencia })
            .IsUnique()
            .HasDatabaseName("UX_MensajesOutbox_EmpresaId_ClaveIdempotencia");

        builder.HasIndex(x => new { x.Estado, x.DisponibleDesdeUtc, x.Id })
            .HasDatabaseName("IX_MensajesOutbox_Claim");

        builder.HasIndex(x => new { x.EmpresaId, x.Estado, x.DisponibleDesdeUtc })
            .HasDatabaseName("IX_MensajesOutbox_EmpresaId_Estado_Disponible");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
