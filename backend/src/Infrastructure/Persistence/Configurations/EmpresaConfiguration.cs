using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas", table =>
        {
            table.HasCheckConstraint(
                "CK_Empresas_Nombre_NoVacio",
                "CHAR_LENGTH(TRIM(`Nombre`)) > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Rtn)
            .HasColumnName("RTN")
            .HasMaxLength(50);

        builder.Property(x => x.Direccion)
            .HasMaxLength(500);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.LogoPublicId)
            .HasMaxLength(250);

        builder.Property(x => x.Activa)
            .HasDefaultValue(true);

        builder.Property(x => x.CreadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.ActualizadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.HasIndex(x => new { x.Activa, x.Nombre })
            .HasDatabaseName("IX_Empresas_Activa_Nombre");

        builder.HasIndex(x => x.Rtn)
            .IsUnique()
            .HasDatabaseName("UX_Empresas_RTN");
    }
}
