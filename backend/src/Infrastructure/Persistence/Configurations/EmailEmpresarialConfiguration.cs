using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N7.7.C — persistencia tenant-safe del ciclo durable de email empresarial.
/// La idempotencia se acota por empresa y los índices de claim/tracking
/// soportan worker, seguimiento y bounce sin almacenar secretos del proveedor.
/// </summary>
public sealed class EmailEmpresarialConfiguration : IEntityTypeConfiguration<EmailEmpresarial>
{
    public void Configure(EntityTypeBuilder<EmailEmpresarial> builder)
    {
        builder.ToTable("EmailsEmpresariales", table =>
        {
            table.HasCheckConstraint(
                "CK_EmailsEmpresariales_EmpresaId_Positivo",
                "`EmpresaId` > 0");
            table.HasCheckConstraint(
                "CK_EmailsEmpresariales_Destinatario_NoVacio",
                "CHAR_LENGTH(TRIM(`Destinatario`)) > 0");
            table.HasCheckConstraint(
                "CK_EmailsEmpresariales_ClaveIdempotencia_NoVacia",
                "CHAR_LENGTH(TRIM(`ClaveIdempotencia`)) > 0");
            table.HasCheckConstraint(
                "CK_EmailsEmpresariales_Estado_Valido",
                "`Estado` BETWEEN 0 AND 6");
            table.HasCheckConstraint(
                "CK_EmailsEmpresariales_Intentos_NoNegativo",
                "`Intentos` >= 0");
            table.HasCheckConstraint(
                "CK_EmailsEmpresariales_Contenido_O_Plantilla",
                "((`PlantillaCodigo` IS NULL AND `PlantillaVersion` IS NULL AND `VariablesJson` IS NULL AND `Asunto` IS NOT NULL AND `CuerpoHtml` IS NOT NULL) OR (`PlantillaCodigo` IS NOT NULL AND `PlantillaVersion` IS NOT NULL AND `PlantillaVersion` > 0 AND `VariablesJson` IS NOT NULL AND `Asunto` IS NULL AND `CuerpoHtml` IS NULL AND `CuerpoTexto` IS NULL))");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MensajeId)
            .IsRequired()
            .HasColumnType("char(36)");
        builder.Property(x => x.EmpresaId).IsRequired();
        builder.Property(x => x.Destinatario)
            .IsRequired()
            .HasMaxLength(EmailEmpresarial.LongitudMaximaDestinatario);
        builder.Property(x => x.Asunto)
            .HasMaxLength(EmailEmpresarial.LongitudMaximaAsunto);
        builder.Property(x => x.CuerpoHtml).HasColumnType("longtext");
        builder.Property(x => x.CuerpoTexto).HasColumnType("longtext");
        builder.Property(x => x.PlantillaCodigo)
            .HasMaxLength(EmailEmpresarial.LongitudMaximaCodigoPlantilla);
        builder.Property(x => x.PlantillaVersion);
        builder.Property(x => x.VariablesJson).HasColumnType("longtext");
        builder.Property(x => x.ClaveIdempotencia)
            .IsRequired()
            .HasMaxLength(EmailEmpresarial.LongitudMaximaClaveIdempotencia);
        builder.Property(x => x.CorrelationId)
            .HasMaxLength(EmailEmpresarial.LongitudMaximaCorrelationId);

        builder.Property(x => x.Estado)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(EstadoEntregaEmail.Pendiente);
        builder.Property(x => x.Intentos)
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(x => x.CreadoEnUtc).IsRequired();
        builder.Property(x => x.DisponibleDesdeUtc).IsRequired();
        builder.Property(x => x.ProcesandoDesdeUtc);
        builder.Property(x => x.AceptadoProveedorEnUtc);
        builder.Property(x => x.EntregadoEnUtc);
        builder.Property(x => x.RebotadoEnUtc);
        builder.Property(x => x.UltimoIntentoEnUtc);
        builder.Property(x => x.ProviderMessageId)
            .HasMaxLength(EmailEmpresarial.LongitudMaximaProviderMessageId);
        builder.Property(x => x.TipoRebote).HasConversion<int?>();
        builder.Property(x => x.CodigoRebote)
            .HasMaxLength(EmailEmpresarial.LongitudMaximaCodigoRebote);
        builder.Property(x => x.UltimoError)
            .HasMaxLength(EmailEmpresarial.LongitudMaximaError);

        builder.Ignore(x => x.EsDespachoTerminal);
        builder.Ignore(x => x.EsEntregaTerminal);

        builder.HasIndex(x => new { x.EmpresaId, x.MensajeId })
            .IsUnique()
            .HasDatabaseName("UX_EmailsEmpresariales_Empresa_MensajeId");
        builder.HasIndex(x => new { x.EmpresaId, x.ClaveIdempotencia })
            .IsUnique()
            .HasDatabaseName("UX_EmailsEmpresariales_Empresa_Idempotencia");
        builder.HasIndex(x => new { x.Estado, x.DisponibleDesdeUtc, x.Id })
            .HasDatabaseName("IX_EmailsEmpresariales_Claim");
        builder.HasIndex(x => new { x.EmpresaId, x.Estado, x.DisponibleDesdeUtc })
            .HasDatabaseName("IX_EmailsEmpresariales_Empresa_Estado_Disponible");
        builder.HasIndex(x => new { x.EmpresaId, x.ProviderMessageId })
            .IsUnique()
            .HasDatabaseName("UX_EmailsEmpresariales_Empresa_ProviderMessageId");
        builder.HasIndex(x => new { x.EmpresaId, x.CorrelationId })
            .HasDatabaseName("IX_EmailsEmpresariales_Empresa_Correlation");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// N7.7.C — versión inmutable de plantilla por tenant. Código + versión es la
/// identidad funcional y permite que mensajes históricos sigan apuntando a la
/// versión exacta que se utilizó.
/// </summary>
public sealed class PlantillaEmailEmpresarialConfiguration : IEntityTypeConfiguration<PlantillaEmailEmpresarial>
{
    public void Configure(EntityTypeBuilder<PlantillaEmailEmpresarial> builder)
    {
        builder.ToTable("PlantillasEmailEmpresarial", table =>
        {
            table.HasCheckConstraint(
                "CK_PlantillasEmail_EmpresaId_Positivo",
                "`EmpresaId` > 0");
            table.HasCheckConstraint(
                "CK_PlantillasEmail_Codigo_NoVacio",
                "CHAR_LENGTH(TRIM(`Codigo`)) > 0");
            table.HasCheckConstraint(
                "CK_PlantillasEmail_Version_Positiva",
                "`Version` > 0");
            table.HasCheckConstraint(
                "CK_PlantillasEmail_Nombre_NoVacio",
                "CHAR_LENGTH(TRIM(`Nombre`)) > 0");
            table.HasCheckConstraint(
                "CK_PlantillasEmail_Asunto_NoVacio",
                "CHAR_LENGTH(TRIM(`AsuntoPlantilla`)) > 0");
            table.HasCheckConstraint(
                "CK_PlantillasEmail_Html_NoVacio",
                "CHAR_LENGTH(TRIM(`CuerpoHtmlPlantilla`)) > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmpresaId).IsRequired();
        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(PlantillaEmailEmpresarial.LongitudMaximaCodigo);
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(PlantillaEmailEmpresarial.LongitudMaximaNombre);
        builder.Property(x => x.AsuntoPlantilla)
            .IsRequired()
            .HasMaxLength(PlantillaEmailEmpresarial.LongitudMaximaAsunto);
        builder.Property(x => x.CuerpoHtmlPlantilla)
            .IsRequired()
            .HasColumnType("longtext");
        builder.Property(x => x.CuerpoTextoPlantilla)
            .HasColumnType("longtext");
        builder.Property(x => x.Activa)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(x => new { x.EmpresaId, x.Codigo, x.Version })
            .IsUnique()
            .HasDatabaseName("UX_PlantillasEmail_Empresa_Codigo_Version");
        builder.HasIndex(x => new { x.EmpresaId, x.Codigo, x.Activa })
            .HasDatabaseName("IX_PlantillasEmail_Empresa_Codigo_Activa");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
