using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.2.C — persistencia de CuentaBancaria.
/// Mantiene Banco como autoridad relacional y no introduce un ledger paralelo;
/// MovimientoFinanciero continúa siendo la autoridad de movimientos financieros.
/// </summary>
public sealed class CuentaBancariaConfiguration : IEntityTypeConfiguration<CuentaBancaria>
{
    public void Configure(EntityTypeBuilder<CuentaBancaria> builder)
    {
        builder.ToTable("CuentasBancarias", table =>
        {
            table.HasCheckConstraint("CK_CuentasBancarias_BancoId", "`BancoId` > 0");
            table.HasCheckConstraint("CK_CuentasBancarias_SaldoInicial", "`SaldoInicial` >= 0");
            table.HasCheckConstraint("CK_CuentasBancarias_Estado", "`Estado` IN (1, 2)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BancoId)
            .IsRequired();

        builder.HasOne<Banco>()
            .WithMany()
            .HasForeignKey(x => x.BancoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.NumeroCuenta)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Moneda)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.SaldoInicial)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Estado)
            .IsRequired();

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.BancoId)
            .HasDatabaseName("IX_CuentasBancarias_BancoId");

        builder.HasIndex(x => new { x.BancoId, x.NumeroCuenta })
            .IsUnique()
            .HasDatabaseName("UX_CuentasBancarias_BancoId_NumeroCuenta");

        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_CuentasBancarias_Estado");
    }
}
