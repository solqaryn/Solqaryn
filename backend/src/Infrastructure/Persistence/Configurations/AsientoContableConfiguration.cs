using Solqaryn.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.7.C — persistencia del encabezado de asientos contables.
/// Mantiene numeración opcional única, trazabilidad de origen y detalle dependiente sin cascadas ambiguas.
/// </summary>
public sealed class AsientoContableConfiguration : IEntityTypeConfiguration<AsientoContable>
{
    public void Configure(EntityTypeBuilder<AsientoContable> builder)
    {
        builder.ToTable("AsientosContables", table =>
        {
            table.HasCheckConstraint("CK_AsientosContables_Concepto", "CHAR_LENGTH(TRIM(`Concepto`)) > 0");
        });

        builder.Property(x => x.Fecha)
            .IsRequired();

        builder.Property(x => x.Concepto)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Numero)
            .HasMaxLength(50);

        builder.Property(x => x.TipoDocumentoOrigen)
            .HasMaxLength(100);

        builder.Property(x => x.CreadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.ActualizadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.HasIndex(x => x.Numero)
            .IsUnique()
            .HasDatabaseName("UX_AsientosContables_Numero");

        builder.HasIndex(x => x.Fecha)
            .HasDatabaseName("IX_AsientosContables_Fecha");

        builder.HasIndex(x => new { x.TipoDocumentoOrigen, x.DocumentoOrigenId })
            .HasDatabaseName("IX_AsientosContables_Origen");

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.AsientoContable)
            .HasForeignKey(x => x.AsientoContableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Detalles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
