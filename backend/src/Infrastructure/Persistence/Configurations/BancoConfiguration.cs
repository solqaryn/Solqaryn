using Solqaryn.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class BancoConfiguration : IEntityTypeConfiguration<Banco>
{
    public void Configure(EntityTypeBuilder<Banco> builder)
    {
        builder.ToTable("Bancos");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.CodigoNormalizado)
            .HasMaxLength(50)
            .HasComputedColumnSql("LOWER(TRIM(Codigo))", stored: true);
        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(120);
        builder.Property(x => x.SwiftBic)
            .HasMaxLength(20);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);

        builder.HasIndex(x => x.CodigoNormalizado)
            .IsUnique()
            .HasDatabaseName("UX_Bancos_Codigo_Normalizado");
        builder.HasIndex(x => x.Nombre)
            .HasDatabaseName("IX_Bancos_Nombre");
        builder.HasIndex(x => new { x.Activo, x.Eliminado })
            .HasDatabaseName("IX_Bancos_Estado");

        // No se aplica query filter: pagos históricos deben poder resolver el banco
        // aunque el catálogo sea desactivado o eliminado lógicamente. La elegibilidad
        // para operaciones nuevas se valida explícitamente en repositorio/servicio.
    }
}
