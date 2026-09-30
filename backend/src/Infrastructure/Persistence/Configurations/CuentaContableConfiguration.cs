using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.6.C — persistencia jerárquica del plan de cuentas.
/// Mantiene una única autoridad por Código y evita borrado en cascada de subcuentas.
/// </summary>
public sealed class CuentaContableConfiguration : IEntityTypeConfiguration<CuentaContable>
{
    public void Configure(EntityTypeBuilder<CuentaContable> builder)
    {
        builder.ToTable("CuentasContables", table =>
        {
            table.HasCheckConstraint("CK_CuentasContables_Codigo", "CHAR_LENGTH(TRIM(`Codigo`)) > 0");
            table.HasCheckConstraint("CK_CuentasContables_Nombre", "CHAR_LENGTH(TRIM(`Nombre`)) > 0");
            table.HasCheckConstraint("CK_CuentasContables_Tipo", "`Tipo` BETWEEN 1 AND 6");
        });

        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Descripcion)
            .HasMaxLength(1000);

        builder.Property(x => x.CreadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.ActualizadoPorNombreUsuario)
            .HasMaxLength(150);

        builder.Property(x => x.AceptaMovimientos)
            .HasDefaultValue(true);

        builder.Property(x => x.Activa)
            .HasDefaultValue(true);

        builder.HasIndex(x => x.Codigo)
            .IsUnique()
            .HasDatabaseName("UX_CuentasContables_Codigo");

        builder.HasIndex(x => x.CuentaPadreId)
            .HasDatabaseName("IX_CuentasContables_CuentaPadreId");

        builder.HasIndex(x => new { x.Tipo, x.Activa })
            .HasDatabaseName("IX_CuentasContables_Tipo_Activa");

        builder.HasOne(x => x.CuentaPadre)
            .WithMany(x => x.Subcuentas)
            .HasForeignKey(x => x.CuentaPadreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
