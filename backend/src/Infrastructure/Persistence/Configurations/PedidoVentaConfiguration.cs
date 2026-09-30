using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class PedidoVentaConfiguration : IEntityTypeConfiguration<PedidoVenta>
{
    public void Configure(EntityTypeBuilder<PedidoVenta> builder)
    {
        builder.ToTable("PedidosVenta", table =>
        {
            table.HasCheckConstraint(
                "CK_PedidosVenta_Estado",
                $"`Estado` IN ({(int)EstadoPedidoVenta.Borrador}, {(int)EstadoPedidoVenta.Confirmado}, {(int)EstadoPedidoVenta.Anulado})");
            table.HasCheckConstraint(
                "CK_PedidosVenta_Idempotencia_Atomica",
                "((`IdempotencyKey` IS NULL AND `IdempotencyFingerprint` IS NULL) OR (`IdempotencyKey` IS NOT NULL AND `IdempotencyFingerprint` IS NOT NULL))");
            table.HasCheckConstraint(
                "CK_PedidosVenta_IdempotencyFingerprint_Sha256",
                "(`IdempotencyFingerprint` IS NULL OR (CHAR_LENGTH(`IdempotencyFingerprint`) = 64 AND `IdempotencyFingerprint` REGEXP '^[0-9a-f]{64}$'))");
        });

        builder.Property(p => p.Estado).HasConversion<int>().IsRequired();
        builder.Property(p => p.ClienteNombreSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ClienteDocumentoSnapshot).HasMaxLength(50);
        builder.Property(p => p.Observaciones).HasMaxLength(1000);
        builder.Property(p => p.IdempotencyKey).HasMaxLength(128);
        builder.Property(p => p.IdempotencyFingerprint).HasMaxLength(64).IsFixedLength();

        builder.HasIndex(p => p.CotizacionId)
            .IsUnique()
            .HasDatabaseName("UX_PedidosVenta_CotizacionId");
        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_PedidosVenta_IdempotencyKey");
        builder.HasIndex(p => new { p.ClienteId, p.Estado })
            .HasDatabaseName("IX_PedidosVenta_Cliente_Estado");

        builder.HasOne(p => p.Cotizacion)
            .WithMany()
            .HasForeignKey(p => p.CotizacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Cliente)
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Detalles)
            .WithOne(d => d.PedidoVenta)
            .HasForeignKey(d => d.PedidoVentaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
