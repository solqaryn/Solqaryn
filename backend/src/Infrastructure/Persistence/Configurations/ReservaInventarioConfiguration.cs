using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica del documento de reserva ERP-N1.8.
/// La reserva puede abarcar múltiples claves físicas; cada detalle conserva
/// su almacén/ubicación y la disponibilidad se controla contra ExistenciaVariante.
/// </summary>
public sealed class ReservaInventarioConfiguration : IEntityTypeConfiguration<ReservaInventario>
{
    public void Configure(EntityTypeBuilder<ReservaInventario> builder)
    {
        builder.ToTable("ReservasInventario");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Numero).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.MotivoLiberacion).HasMaxLength(500);
        builder.Property(x => x.MotivoCancelacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.Numero)
            .IsUnique()
            .HasDatabaseName("UX_ReservasInventario_Numero");
        builder.HasIndex(x => new { x.Estado, x.FechaExpiracion })
            .HasDatabaseName("IX_ReservasInventario_Estado_Expiracion");
        builder.HasIndex(x => x.PedidoVentaId)
            .IsUnique()
            .HasDatabaseName("UX_ReservasInventario_PedidoVentaId");
        builder.HasIndex(x => x.VentaId)
            .HasDatabaseName("IX_ReservasInventario_VentaId");

        builder.HasOne(x => x.PedidoVenta)
            .WithMany()
            .HasForeignKey(x => x.PedidoVentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReservasInventario_PedidosVenta_PedidoVentaId");

        builder.HasOne(x => x.Venta)
            .WithMany()
            .HasForeignKey(x => x.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ReservasInventario_Ventas_VentaId");

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.ReservaInventario)
            .HasForeignKey(x => x.ReservaInventarioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ReservaInventarioDetalles_ReservasInventario_ReservaInventarioId");
    }
}
