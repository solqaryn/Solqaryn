using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class PlataformaSesionSoporteConfiguration
    : IEntityTypeConfiguration<PlataformaSesionSoporte>
{
    public void Configure(EntityTypeBuilder<PlataformaSesionSoporte> builder)
    {
        builder.ToTable("PlataformaSesionesSoporte", table =>
        {
            table.HasCheckConstraint(
                "CK_PlataformaSesionesSoporte_Modulo",
                "`Modulo` IN (2,3)");
            table.HasCheckConstraint(
                "CK_PlataformaSesionesSoporte_Motivo",
                "CHAR_LENGTH(TRIM(`Motivo`)) BETWEEN 10 AND 500");
            table.HasCheckConstraint(
                "CK_PlataformaSesionesSoporte_Vigencia",
                "`ExpiraUtc` > `CreadaUtc`");
            table.HasCheckConstraint(
                "CK_PlataformaSesionesSoporte_Revocacion",
                "(`RevocadaUtc` IS NULL AND `RevocadaPorUsuarioId` IS NULL) OR " +
                "(`RevocadaUtc` IS NOT NULL AND `RevocadaPorUsuarioId` IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_PlataformaSesionesSoporte");
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Modulo).HasConversion<int>().IsRequired();
        builder.Property(x => x.Motivo).IsRequired().HasMaxLength(500);
        builder.Property(x => x.TokenHash).HasColumnType("char(64)").IsRequired().HasMaxLength(64)
            .IsUnicode(false);
        builder.Property(x => x.CreadaUtc).HasColumnType("datetime(6)");
        builder.Property(x => x.ExpiraUtc).HasColumnType("datetime(6)");
        builder.Property(x => x.RevocadaUtc).HasColumnType("datetime(6)");
        builder.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(1);

        builder.HasIndex(x => x.TokenHash).IsUnique()
            .HasDatabaseName("UX_PlataformaSesionesSoporte_TokenHash");
        builder.HasIndex(x => new { x.EmpresaId, x.ExpiraUtc, x.RevocadaUtc })
            .HasDatabaseName("IX_PlataformaSesionesSoporte_Empresa_Vigencia");
        builder.HasIndex(x => new { x.OperadorUsuarioId, x.CreadaUtc })
            .HasDatabaseName("IX_PlataformaSesionesSoporte_Operador_Creada");

        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.OperadorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PlataformaSesionesSoporte_Usuarios_Operador");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PlataformaSesionesSoporte_Empresas_Empresa");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RevocadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PlataformaSesionesSoporte_Usuarios_Revocador");
    }
}