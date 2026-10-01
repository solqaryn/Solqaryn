using Solqaryn.Domain.Entities.Bancos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class MovimientoEstadoCuentaConfiguration : IEntityTypeConfiguration<MovimientoEstadoCuenta>
{
    public void Configure(EntityTypeBuilder<MovimientoEstadoCuenta> builder)
    {
        builder.ToTable("MovimientosEstadoCuenta", table =>
        {
            table.HasCheckConstraint("CK_MovimientosEstadoCuenta_ConciliacionBancariaId", "`ConciliacionBancariaId` > 0");
            table.HasCheckConstraint("CK_MovimientosEstadoCuenta_Monto", "`Monto` > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConciliacionBancariaId).IsRequired();
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.FechaMovimiento).IsRequired();
        builder.Property(x => x.Concepto).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Referencia).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Tipo).IsRequired();
        builder.Property(x => x.Monto).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Estado).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.ConciliacionBancariaId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_MovimientosEstadoCuenta_Conciliacion_IdempotencyKey");
        builder.HasIndex(x => new { x.ConciliacionBancariaId, x.FechaMovimiento })
            .HasDatabaseName("IX_MovimientosEstadoCuenta_Conciliacion_Fecha");
        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_MovimientosEstadoCuenta_Estado");
    }
}
