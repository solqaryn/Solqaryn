using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia de pagos, anticipos, retenciones y notas de crédito aplicadas a una cuenta por pagar.
/// </summary>
public sealed class AplicacionCuentaPorPagarConfiguration : IEntityTypeConfiguration<AplicacionCuentaPorPagar>
{
    public void Configure(EntityTypeBuilder<AplicacionCuentaPorPagar> builder)
    {
        builder.ToTable("AplicacionesCuentaPorPagar", table =>
        {
            table.HasCheckConstraint("CK_AplicacionesCuentaPorPagar_CuentaValida",
                "CuentaPorPagarId > 0");
            table.HasCheckConstraint("CK_AplicacionesCuentaPorPagar_TipoValido",
                "Tipo IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_AplicacionesCuentaPorPagar_MontoPositivo",
                "Monto > 0");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Tipo).HasConversion<int>().IsRequired();
        builder.Property(x => x.Monto).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReferenciaExterna).HasMaxLength(150);
        builder.Property(x => x.MotivoReversion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.CuentaPorPagarId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_AplicacionesCuentaPorPagar_Cuenta_IdempotencyKey");

        builder.HasIndex(x => new { x.CuentaPorPagarId, x.FechaAplicacionUtc })
            .HasDatabaseName("IX_AplicacionesCuentaPorPagar_Cuenta_Fecha");

        builder.HasIndex(x => new { x.Tipo, x.FechaAplicacionUtc })
            .HasDatabaseName("IX_AplicacionesCuentaPorPagar_Tipo_Fecha");
    }
}
