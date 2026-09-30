using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class CargaMasivaConfiguration : IEntityTypeConfiguration<CargaMasiva>
{
    public void Configure(EntityTypeBuilder<CargaMasiva> builder)
    {
        builder.ToTable("CargasMasivas");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.NombreArchivo).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Extension).HasMaxLength(12).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.HashArchivo).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DatosNormalizadosJson).HasColumnType("longtext").IsRequired();
        builder.Property(x => x.ConfirmadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ErrorGeneral).HasMaxLength(1000);

        builder.HasIndex(x => new { x.Tipo, x.HashArchivo }).IsUnique();
        builder.HasIndex(x => new { x.Estado, x.FechaCreacion });

        builder.HasMany(x => x.Errores)
            .WithOne(x => x.CargaMasiva)
            .HasForeignKey(x => x.CargaMasivaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
