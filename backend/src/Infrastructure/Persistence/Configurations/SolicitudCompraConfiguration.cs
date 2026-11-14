using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de la cabecera de solicitud de compra ERP-N2.1.
/// La aprobación sigue siendo documental: no genera stock, Kardex, costeo ni finanzas.
/// </summary>
public sealed class SolicitudCompraConfiguration : IEntityTypeConfiguration<SolicitudCompra>
{
    public void Configure(EntityTypeBuilder<SolicitudCompra> builder)
    {
        builder.ToTable("SolicitudesCompra");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NumeroSolicitud).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.Notas).HasMaxLength(1000);
        builder.Property(x => x.SolicitadaPorNombreSnapshot).HasMaxLength(150);
        builder.Property(x => x.DecididaPorNombreSnapshot).HasMaxLength(150);
        builder.Property(x => x.MotivoRechazo).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.NumeroSolicitud)
            .IsUnique()
            .HasDatabaseName("UX_SolicitudesCompra_NumeroSolicitud");

        builder.HasIndex(x => new { x.Estado, x.FechaSolicitudUtc })
            .HasDatabaseName("IX_SolicitudesCompra_Estado_FechaSolicitud");

        builder.HasIndex(x => x.ProveedorId)
            .HasDatabaseName("IX_SolicitudesCompra_ProveedorId");

        builder.HasOne(x => x.Proveedor)
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SolicitudesCompra_Proveedores_ProveedorId");

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.SolicitudCompra)
            .HasForeignKey(x => x.SolicitudCompraId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_SolicitudCompraDetalles_SolicitudesCompra_SolicitudCompraId");
    }
}
