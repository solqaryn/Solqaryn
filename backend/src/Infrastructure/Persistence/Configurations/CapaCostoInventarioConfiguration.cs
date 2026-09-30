using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class CapaCostoInventarioConfiguration : IEntityTypeConfiguration<CapaCostoInventario>
{
    public void Configure(EntityTypeBuilder<CapaCostoInventario> builder)
    {
        builder.ToTable("CapasCostoInventario", t =>
        {
            t.HasCheckConstraint("CK_CapasCosto_Cantidades", "`CantidadOriginal` > 0 AND `CantidadRestante` >= 0 AND `CantidadRestante` <= `CantidadOriginal`");
            t.HasCheckConstraint("CK_CapasCosto_Costo", "`CostoUnitario` >= 0");
            t.HasCheckConstraint("CK_CapasCosto_Origen", "(`EsApertura` = 1 AND `MovimientoInventarioOrigenId` IS NULL AND `CapaCostoOrigenId` IS NULL AND `MotivoApertura` IS NOT NULL) OR (`EsApertura` = 0 AND `MovimientoInventarioOrigenId` IS NOT NULL AND `MotivoApertura` IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductoVarianteId).IsRequired();
        builder.Property(x => x.AlmacenId).IsRequired();
        builder.Property(x => x.UbicacionAlmacenId);
        builder.Property(x => x.MovimientoInventarioOrigenId);
        builder.Property(x => x.CapaCostoOrigenId);
        builder.Property(x => x.EsApertura).HasDefaultValue(false);
        builder.Property(x => x.MotivoApertura).HasMaxLength(500);
        builder.Property(x => x.CantidadOriginal).IsRequired();
        builder.Property(x => x.CantidadRestante).IsRequired();
        builder.Property(x => x.CostoUnitario).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.FechaOrigenUtc).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasAlternateKey(x => new { x.ProductoVarianteId, x.Id })
            .HasName("AK_CapasCosto_Variante_Id");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.AlmacenId, x.UbicacionAlmacenId, x.FechaOrigenUtc, x.Id })
            .HasDatabaseName("IX_CapasCosto_FIFO");
        builder.HasIndex(x => x.MovimientoInventarioOrigenId)
            .HasDatabaseName("IX_CapasCosto_MovimientoOrigen");
        builder.HasIndex(x => new { x.ProductoVarianteId, x.CapaCostoOrigenId })
            .HasDatabaseName("IX_CapasCosto_Linaje");
        builder.HasIndex(x => new { x.AlmacenId, x.UbicacionAlmacenId })
            .HasDatabaseName("IX_CapasCosto_Almacen_Ubicacion");

        builder.HasOne<ProductoVariante>()
            .WithMany()
            .HasForeignKey(x => x.ProductoVarianteId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CapasCosto_ProductoVariantes");
        builder.HasOne<Almacen>()
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CapasCosto_Almacenes");
        builder.HasOne<UbicacionAlmacen>()
            .WithMany()
            .HasForeignKey(x => new { x.AlmacenId, x.UbicacionAlmacenId })
            .HasPrincipalKey(x => new { x.AlmacenId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CapasCosto_Ubicacion_MismoAlmacen");
        builder.HasOne<MovimientoInventario>()
            .WithMany()
            .HasForeignKey(x => x.MovimientoInventarioOrigenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CapasCosto_MovimientoOrigen");
        builder.HasOne<CapaCostoInventario>()
            .WithMany()
            .HasForeignKey(x => new { x.ProductoVarianteId, x.CapaCostoOrigenId })
            .HasPrincipalKey(x => new { x.ProductoVarianteId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CapasCosto_CapaOrigen_MismaVariante");
    }
}
