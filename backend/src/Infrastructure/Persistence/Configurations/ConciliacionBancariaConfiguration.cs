using Solqaryn.Domain.Entities.Bancos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class ConciliacionBancariaConfiguration : IEntityTypeConfiguration<ConciliacionBancaria>
{
    public void Configure(EntityTypeBuilder<ConciliacionBancaria> builder)
    {
        builder.ToTable("ConciliacionesBancarias", table =>
        {
            table.HasCheckConstraint("CK_ConciliacionesBancarias_CuentaBancariaId", "`CuentaBancariaId` > 0");
            table.HasCheckConstraint("CK_ConciliacionesBancarias_SaldoInicialBanco", "`SaldoInicialBanco` >= 0");
            table.HasCheckConstraint("CK_ConciliacionesBancarias_SaldoFinalBanco", "`SaldoFinalBanco` >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CuentaBancariaId).IsRequired();
        builder.Property(x => x.FechaInicio).IsRequired();
        builder.Property(x => x.FechaFin).IsRequired();
        builder.Property(x => x.SaldoInicialBanco).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.SaldoFinalBanco).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Estado).IsRequired();
        builder.Property(x => x.Observaciones).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasOne(x => x.CuentaBancaria)
            .WithMany()
            .HasForeignKey(x => x.CuentaBancariaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Movimientos)
            .WithOne()
            .HasForeignKey(x => x.ConciliacionBancariaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CuentaBancariaId)
            .HasDatabaseName("IX_ConciliacionesBancarias_CuentaBancariaId");
        builder.HasIndex(x => new { x.CuentaBancariaId, x.FechaInicio, x.FechaFin })
            .HasDatabaseName("IX_ConciliacionesBancarias_Cuenta_Periodo");
        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_ConciliacionesBancarias_Estado");
    }
}
