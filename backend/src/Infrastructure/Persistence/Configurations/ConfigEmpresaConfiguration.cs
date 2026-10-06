using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class ConfigEmpresaConfiguration : IEntityTypeConfiguration<ConfigEmpresa>
{
    public void Configure(EntityTypeBuilder<ConfigEmpresa> builder)
    {
        builder.ToTable("ConfigEmpresas", table =>
        {
            table.HasCheckConstraint(
                "CK_ConfigEmpresas_Moneda",
                "CHAR_LENGTH(TRIM(`Moneda`)) = 3");
            table.HasCheckConstraint(
                "CK_ConfigEmpresas_Version",
                "`Version` > 0");
        });

        builder.HasKey(x => x.Id);

        // PlantillaCorreoEmpresa is owned by the tenant Empresa through EmpresaId.
        // Ignoring this convenience collection prevents EF conventions from creating
        // a second shadow relationship/FK (ConfigEmpresaId) that is not part of the
        // N6.7.C migration or the canonical tenant persistence contract.
        builder.Ignore(x => x.PlantillasCorreo);

        builder.Property(x => x.Moneda)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("HNL");

        builder.Property(x => x.ZonaHoraria)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("America/Tegucigalpa");

        builder.Property(x => x.ImpuestosJson)
            .IsRequired()
            .HasColumnType("longtext")
            .HasDefaultValueSql("('{}')");

        builder.Property(x => x.EmisionJson)
            .IsRequired()
            .HasColumnType("longtext")
            .HasDefaultValueSql("('{}')");

        builder.Property(x => x.CorreoRemitente)
            .HasMaxLength(254);

        builder.Property(x => x.CorreoNombreRemitente)
            .HasMaxLength(200);

        builder.Property(x => x.CorreoSecretoReferencia)
            .HasMaxLength(500);

        builder.Property(x => x.Version)
            .HasDefaultValue(1L)
            .IsConcurrencyToken();

        builder.Property(x => x.CreadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.ActualizadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.HasOne(x => x.Empresa)
            .WithOne()
            .HasForeignKey<ConfigEmpresa>(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmpresaId)
            .IsUnique()
            .HasDatabaseName("UX_ConfigEmpresas_EmpresaId");
    }
}

public sealed class PlantillaCorreoEmpresaConfiguration : IEntityTypeConfiguration<PlantillaCorreoEmpresa>
{
    public void Configure(EntityTypeBuilder<PlantillaCorreoEmpresa> builder)
    {
        builder.ToTable("PlantillasCorreoEmpresa");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TipoPlantilla)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Asunto)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Cuerpo)
            .IsRequired()
            .HasColumnType("longtext");

        builder.Property(x => x.CreadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.ActualizadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.HasOne(x => x.Empresa)
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EmpresaId, x.TipoPlantilla })
            .IsUnique()
            .HasDatabaseName("UX_PlantillasCorreoEmpresa_Empresa_Tipo");
    }
}
