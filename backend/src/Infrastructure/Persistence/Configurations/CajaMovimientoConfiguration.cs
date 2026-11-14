using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Entities.Cajas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.1.C — persistencia grounded de movimientos de caja.
/// No enlaza movimientos con Venta/Factura/Pago de forma automática.
/// </summary>
public sealed class CajaMovimientoConfiguration : IEntityTypeConfiguration<CajaMovimiento>
{
    public void Configure(EntityTypeBuilder<CajaMovimiento> builder)
    {
        builder.ToTable("CajaMovimientos", table =>
        {
            table.HasCheckConstraint("CK_CajaMovimientos_CajaSesionId", "`CajaSesionId` > 0");
            table.HasCheckConstraint("CK_CajaMovimientos_UsuarioId", "`UsuarioId` > 0");
            table.HasCheckConstraint("CK_CajaMovimientos_Tipo", "`Tipo` IN (1, 2, 3, 4, 5)");
            table.HasCheckConstraint("CK_CajaMovimientos_Monto", "`Monto` > 0");
            table.HasCheckConstraint("CK_CajaMovimientos_Referencia", "CHAR_LENGTH(TRIM(`Referencia`)) > 0");
        });

        builder.Property(x => x.Tipo).IsRequired();
        builder.Property(x => x.Monto).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.Referencia).IsRequired();
        builder.Property(x => x.FechaOperacion).IsRequired();

        builder.HasIndex(x => new { x.CajaSesionId, x.FechaOperacion })
            .HasDatabaseName("IX_CajaMovimientos_Sesion_Fecha");
        builder.HasIndex(x => x.UsuarioId)
            .HasDatabaseName("IX_CajaMovimientos_UsuarioId");
        builder.HasIndex(x => x.Tipo)
            .HasDatabaseName("IX_CajaMovimientos_Tipo");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
