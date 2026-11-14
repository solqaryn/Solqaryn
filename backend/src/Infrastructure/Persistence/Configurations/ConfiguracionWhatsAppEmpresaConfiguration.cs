using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N7.6.C — persistencia tenant-safe de la configuración de proveedor WhatsApp.
/// Las credenciales persistidas son únicamente referencias opacas a almacenamiento seguro.
/// </summary>
public sealed class ConfiguracionWhatsAppEmpresaConfiguration : IEntityTypeConfiguration<ConfiguracionWhatsAppEmpresa>
{
    public void Configure(EntityTypeBuilder<ConfiguracionWhatsAppEmpresa> builder)
    {
        builder.ToTable("ConfiguracionesWhatsAppEmpresa", table =>
        {
            table.HasCheckConstraint(
                "CK_ConfiguracionesWhatsAppEmpresa_EmpresaId_Positivo",
                "`EmpresaId` > 0");
            table.HasCheckConstraint(
                "CK_ConfiguracionesWhatsAppEmpresa_Numero_E164",
                "`NumeroTelefonoE164` REGEXP '^\\+[0-9]{8,15}$'");
            table.HasCheckConstraint(
                "CK_ConfiguracionesWhatsAppEmpresa_Token_Referencia",
                "INSTR(`TokenSecretoReferencia`, '://') > 1");
            table.HasCheckConstraint(
                "CK_ConfiguracionesWhatsAppEmpresa_Webhook_Referencia",
                "INSTR(`WebhookSecretoReferencia`, '://') > 1");
            table.HasCheckConstraint(
                "CK_ConfiguracionesWhatsAppEmpresa_Version_Positiva",
                "`Version` > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmpresaId)
            .IsRequired();

        builder.Property(x => x.NumeroTelefonoE164)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(x => x.TokenSecretoReferencia)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.WebhookSecretoReferencia)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Activa)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Version)
            .IsRequired()
            .HasDefaultValue(1L)
            .IsConcurrencyToken();

        builder.Property(x => x.CreadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.ActualizadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.HasIndex(x => x.EmpresaId)
            .IsUnique()
            .HasDatabaseName("UX_ConfiguracionesWhatsAppEmpresa_EmpresaId");

        builder.HasOne(x => x.Empresa)
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
