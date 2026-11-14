using Solqaryn.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.7.C — persistencia de líneas Debe/Haber.
/// Garantiza importes no negativos y que cada línea afecte un único lado del asiento.
/// </summary>
public sealed class AsientoDetalleConfiguration : IEntityTypeConfiguration<AsientoDetalle>
{
    public void Configure(EntityTypeBuilder<AsientoDetalle> builder)
    {
        builder.ToTable("AsientoDetalles", table =>
        {
            table.HasCheckConstraint("CK_AsientoDetalles_MontosNoNegativos", "`Debe` >= 0 AND `Haber` >= 0");
            table.HasCheckConstraint("CK_AsientoDetalles_UnSoloLado", "((`Debe` > 0 AND `Haber` = 0) OR (`Haber` > 0 AND `Debe` = 0))");
        });

        builder.Property(x => x.Debe)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Haber)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Referencia)
            .HasMaxLength(250);

        builder.Property(x => x.CreadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.ActualizadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.HasIndex(x => x.AsientoContableId)
            .HasDatabaseName("IX_AsientoDetalles_AsientoContableId");

        builder.HasIndex(x => x.CuentaContableId)
            .HasDatabaseName("IX_AsientoDetalles_CuentaContableId");

        builder.HasOne(x => x.CuentaContable)
            .WithMany()
            .HasForeignKey(x => x.CuentaContableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
