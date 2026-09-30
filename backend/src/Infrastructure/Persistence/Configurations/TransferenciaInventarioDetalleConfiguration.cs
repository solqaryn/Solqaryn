using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class TransferenciaInventarioDetalleConfiguration : IEntityTypeConfiguration<TransferenciaInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<TransferenciaInventarioDetalle> builder)
    {
        builder.ToTable("TransferenciaInventarioDetalles", table =>
        {
            table.HasCheckConstraint(
                "CK_TransferenciaInventarioDetalles_CantidadesNoNegativas",
                "`CantidadSolicitada` > 0 AND `CantidadAprobada` >= 0 AND `CantidadDespachada` >= 0 AND `CantidadRecibida` >= 0 AND `CantidadFaltante` >= 0 AND `CantidadSobrante` >= 0 AND `CantidadDanada` >= 0");
            table.HasCheckConstraint(
                "CK_TransferenciaInventarioDetalles_Aprobada",
                "`CantidadAprobada` <= `CantidadSolicitada`");
            table.HasCheckConstraint(
                "CK_TransferenciaInventarioDetalles_Despachada",
                "`CantidadDespachada` <= `CantidadAprobada`");
            table.HasCheckConstraint(
                "CK_TransferenciaInventarioDetalles_Recepcion",
                "`CantidadRecibida` + `CantidadFaltante` + `CantidadDanada` <= `CantidadDespachada`");
        });

        builder.Property(d => d.ProductoSkuSnapshot).HasMaxLength(80);
        builder.Property(d => d.ProductoMarcaSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoModeloSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoColorSnapshot).HasMaxLength(100);
        builder.Property(d => d.ProductoTallaSnapshot).HasMaxLength(100);
        builder.Property(d => d.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(d => d.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Ignore(d => d.RecepcionCerrada);

        builder.HasIndex(d => d.TransferenciaInventarioId)
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_TransferenciaId");
        builder.HasIndex(d => new { d.ProductoVarianteId, d.TransferenciaInventarioId })
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_Variante_Transferencia");
        builder.HasIndex(d => d.UbicacionOrigenId)
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_UbicacionOrigen");
        builder.HasIndex(d => d.UbicacionDestinoId)
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_UbicacionDestino");

        builder.HasOne(d => d.ProductoVariante)
            .WithMany()
            .HasForeignKey(d => d.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TransferenciaInventarioDetalles_ProductoVariantes");

        builder.HasOne(d => d.UbicacionOrigen)
            .WithMany()
            .HasForeignKey(d => d.UbicacionOrigenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TransferenciaInventarioDetalles_Ubicaciones_Origen");

        builder.HasOne(d => d.UbicacionDestino)
            .WithMany()
            .HasForeignKey(d => d.UbicacionDestinoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TransferenciaInventarioDetalles_Ubicaciones_Destino");
    }
}
