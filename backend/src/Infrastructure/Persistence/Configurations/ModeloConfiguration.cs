using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ModeloConfiguration : IEntityTypeConfiguration<Modelo>
{
    public void Configure(EntityTypeBuilder<Modelo> builder)
    {
        builder.ToTable("Modelos");
        // Los IDs copiados del catálogo legacy se preservan durante M1; las altas
        // posteriores deben recibir un nuevo ID autogenerado y nunca reutilizar 0.
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.Property(x => x.NombreMarcaActivoUnico)
            .HasMaxLength(180)
            .HasComputedColumnSql("IF(Eliminado = 0, CONCAT(CAST(MarcaId AS CHAR), ':', LOWER(TRIM(Nombre))), NULL)", stored: true);
        builder.HasQueryFilter(x => !x.Eliminado);
        builder.HasIndex(x => x.NombreMarcaActivoUnico).IsUnique().HasDatabaseName("UX_Modelos_Marca_Nombre_Activo");
        builder.HasIndex(x => new { x.MarcaId, x.Activo, x.Eliminado }).HasDatabaseName("IX_Modelos_Marca_Estado");
        builder.HasOne(x => x.Marca)
            .WithMany(x => x.Modelos)
            .HasForeignKey(x => x.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
