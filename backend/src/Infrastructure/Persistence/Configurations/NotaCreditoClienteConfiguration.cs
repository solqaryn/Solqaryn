using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N3.7.C — persistencia mínima grounded de NotaCreditoCliente.
/// Mantiene fuera de esta fase lifecycle fiscal/numeración, aplicación contable/saldo,
/// idempotencia/cardinalidad, RBAC/HTTP y efectos de inventario/caja.
/// </summary>
public sealed class NotaCreditoClienteConfiguration : IEntityTypeConfiguration<NotaCreditoCliente>
{
    public void Configure(EntityTypeBuilder<NotaCreditoCliente> builder)
    {
        builder.ToTable("NotasCreditoCliente", table =>
        {
            table.HasCheckConstraint("CK_NotasCreditoCliente_FacturaId", "`FacturaId` > 0");
            table.HasCheckConstraint("CK_NotasCreditoCliente_VentaId", "`VentaId` > 0");
            table.HasCheckConstraint("CK_NotasCreditoCliente_Moneda", "CHAR_LENGTH(`Moneda`) = 3");
            table.HasCheckConstraint("CK_NotasCreditoCliente_MontoCredito", "`MontoCredito` > 0");
        });

        builder.Property(x => x.Moneda)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.MontoCredito)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.Motivo)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Observaciones)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.FacturaId)
            .HasDatabaseName("IX_NotasCreditoCliente_FacturaId");

        builder.HasIndex(x => x.VentaId)
            .HasDatabaseName("IX_NotasCreditoCliente_VentaId");

        builder.HasOne(x => x.Factura)
            .WithMany()
            .HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
