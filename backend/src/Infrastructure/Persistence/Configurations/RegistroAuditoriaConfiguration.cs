using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("RegistrosAuditoria", table =>
        {
            table.HasCheckConstraint(
                "CK_RegistrosAuditoria_AmbitoAutoridad",
                "`AmbitoAutoridad` IS NULL OR `AmbitoAutoridad` IN (1, 2)");
            table.HasCheckConstraint(
                "CK_RegistrosAuditoria_OrigenAutorizacion",
                "`OrigenAutorizacion` IS NULL OR `OrigenAutorizacion` IN (1, 2)");
            table.HasCheckConstraint(
                "CK_RegistrosAuditoria_EmpresaId_Positivo",
                "`EmpresaId` IS NULL OR `EmpresaId` > 0");
        });

        builder.Property(r => r.NombreUsuario).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Modulo).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Accion).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.AmbitoAutoridad).HasConversion<int?>();
        builder.Property(r => r.OrigenAutorizacion).HasConversion<int?>();
        builder.Property(r => r.Descripcion).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Entidad).HasMaxLength(80);
        builder.Property(r => r.ValoresAnteriores).HasColumnType("json");
        builder.Property(r => r.ValoresNuevos).HasColumnType("json");
        builder.Property(r => r.Motivo).HasMaxLength(500);
        builder.Property(r => r.Ip).HasMaxLength(64);
        builder.Property(r => r.UserAgent).HasMaxLength(300);
        builder.Property(r => r.CorrelationId).HasMaxLength(64);
        builder.Property(r => r.Resultado).IsRequired().HasMaxLength(20);
        builder.Property(r => r.Error).HasMaxLength(1000);

        builder.HasIndex(r => r.Fecha);
        builder.HasIndex(r => new { r.Modulo, r.ReferenciaId });
        builder.HasIndex(r => r.UsuarioId);
        builder.HasIndex(r => r.EmpresaId).HasDatabaseName("IX_RegistrosAuditoria_EmpresaId");
        builder.HasIndex(r => new { r.AmbitoAutoridad, r.Fecha })
            .HasDatabaseName("IX_RegistrosAuditoria_Ambito_Fecha");
        builder.HasIndex(r => r.Resultado);
        builder.HasIndex(r => r.CorrelationId);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(r => r.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
