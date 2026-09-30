using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class TransferenciaInventarioConfiguration : IEntityTypeConfiguration<TransferenciaInventario>
{
    public void Configure(EntityTypeBuilder<TransferenciaInventario> builder)
    {
        builder.ToTable("TransferenciasInventario", table =>
        {
            table.HasCheckConstraint(
                "CK_TransferenciasInventario_AlmacenesDistintos",
                "`AlmacenOrigenId` <> `AlmacenDestinoId`");
            table.HasCheckConstraint(
                "CK_TransferenciasInventario_Estado_Valido",
                "`Estado` IN ('Borrador','Solicitada','Aprobada','EnTransito','Recibida','Cancelada')");
        });

        builder.Property(t => t.Numero)
            .IsRequired()
            .HasMaxLength(30);
        builder.HasIndex(t => t.Numero)
            .IsUnique()
            .HasDatabaseName("UX_TransferenciasInventario_Numero");

        builder.Property(t => t.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(t => t.Observaciones).HasMaxLength(1000);
        builder.Property(t => t.MotivoCancelacion).HasMaxLength(500);
        builder.Property(t => t.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(t => t.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(t => new { t.Estado, t.FechaSolicitud })
            .HasDatabaseName("IX_TransferenciasInventario_Estado_FechaSolicitud");
        builder.HasIndex(t => new { t.AlmacenOrigenId, t.Estado })
            .HasDatabaseName("IX_TransferenciasInventario_Origen_Estado");
        builder.HasIndex(t => new { t.AlmacenDestinoId, t.Estado })
            .HasDatabaseName("IX_TransferenciasInventario_Destino_Estado");

        builder.HasOne(t => t.AlmacenOrigen)
            .WithMany()
            .HasForeignKey(t => t.AlmacenOrigenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TransferenciasInventario_Almacenes_Origen");

        builder.HasOne(t => t.AlmacenDestino)
            .WithMany()
            .HasForeignKey(t => t.AlmacenDestinoId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_TransferenciasInventario_Almacenes_Destino");

        builder.HasMany(t => t.Detalles)
            .WithOne(d => d.TransferenciaInventario)
            .HasForeignKey(d => d.TransferenciaInventarioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_TransferenciaInventarioDetalles_TransferenciasInventario");
    }
}
