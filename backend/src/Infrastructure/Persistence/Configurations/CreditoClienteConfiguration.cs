using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N3.10.C — persistencia mínima grounded de CreditoCliente.
/// No introduce fórmulas de consumo/disponible, scoring, thresholds adicionales,
/// RBAC, API ni políticas comerciales no certificadas por el dominio.
/// </summary>
public sealed class CreditoClienteConfiguration : IEntityTypeConfiguration<CreditoCliente>
{
    public void Configure(EntityTypeBuilder<CreditoCliente> builder)
    {
        builder.ToTable("CreditosCliente", table =>
        {
            table.HasCheckConstraint("CK_CreditosCliente_ClienteId", "`ClienteId` > 0");
            table.HasCheckConstraint("CK_CreditosCliente_Moneda", "CHAR_LENGTH(`Moneda`) = 3");
            table.HasCheckConstraint("CK_CreditosCliente_LimiteCredito", "`LimiteCredito` >= 0");
            table.HasCheckConstraint("CK_CreditosCliente_DiasCredito", "`DiasCredito` >= 0");
            table.HasCheckConstraint(
                "CK_CreditosCliente_UmbralAlerta",
                "`UmbralAlertaPorcentaje` IS NULL OR (`UmbralAlertaPorcentaje` > 0 AND `UmbralAlertaPorcentaje` <= 100)");
            table.HasCheckConstraint(
                "CK_CreditosCliente_MontoExcepcion",
                "`MontoExcepcion` IS NULL OR `MontoExcepcion` > 0");
        });

        builder.Property(x => x.Moneda)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.LimiteCredito)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.UmbralAlertaPorcentaje)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.MontoExcepcion)
            .HasColumnType("decimal(18,4)");

        builder.HasIndex(x => x.ClienteId)
            .HasDatabaseName("IX_CreditosCliente_ClienteId");

        builder.HasOne(x => x.Cliente)
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
