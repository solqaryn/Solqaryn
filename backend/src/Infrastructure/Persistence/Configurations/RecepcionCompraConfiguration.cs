using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de la cabecera de recepción de mercancía ERP-N2.3.
/// La materialización de stock/Kardex pertenece a N2.3.D y no se modela aquí.
/// </summary>
public sealed class RecepcionCompraConfiguration : IEntityTypeConfiguration<RecepcionCompra>
{
    public void Configure(EntityTypeBuilder<RecepcionCompra> builder)
    {
        builder.ToTable("RecepcionesCompra", table =>
        {
            table.HasCheckConstraint(
                "CK_RecepcionesCompra_IdempotenciaAtomica",
                "(IdempotencyKey IS NULL AND IdempotencyFingerprint IS NULL) OR (IdempotencyKey IS NOT NULL AND CHAR_LENGTH(TRIM(IdempotencyKey)) > 0 AND IdempotencyFingerprint IS NOT NULL AND CHAR_LENGTH(IdempotencyFingerprint) = 64)");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NumeroRecepcion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.Observaciones).HasMaxLength(1000);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.IdempotencyFingerprint).HasMaxLength(64);
        builder.Property(x => x.RecibidaPorNombreSnapshot).HasMaxLength(150);
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Ignore(x => x.EsEditable);
        builder.Ignore(x => x.CantidadRecibidaTotal);
        builder.Ignore(x => x.CantidadAceptadaTotal);
        builder.Ignore(x => x.CantidadDanadaTotal);
        builder.Ignore(x => x.CantidadFaltanteTotal);
        builder.Ignore(x => x.CantidadSobranteTotal);

        builder.HasIndex(x => x.NumeroRecepcion)
            .IsUnique()
            .HasDatabaseName("UX_RecepcionesCompra_NumeroRecepcion");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_RecepcionesCompra_IdempotencyKey");

        builder.HasIndex(x => new { x.OrdenCompraId, x.Estado })
            .HasDatabaseName("IX_RecepcionesCompra_OrdenCompra_Estado");

        builder.HasIndex(x => x.FechaRecepcionUtc)
            .HasDatabaseName("IX_RecepcionesCompra_FechaRecepcionUtc");

        builder.HasOne(x => x.OrdenCompra)
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RecepcionesCompra_OrdenesCompra_OrdenCompraId");

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.RecepcionCompra)
            .HasForeignKey(x => x.RecepcionCompraId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RecepcionCompraDetalles_RecepcionesCompra_RecepcionCompraId");
    }
}
