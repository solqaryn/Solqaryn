using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de la cabecera de orden de compra ERP-N2.2.
/// La orden permanece documental: recepción/stock corresponde a N2.3.
/// </summary>
public sealed class OrdenCompraConfiguration : IEntityTypeConfiguration<OrdenCompra>
{
    public void Configure(EntityTypeBuilder<OrdenCompra> builder)
    {
        builder.ToTable("OrdenesCompra");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.NumeroOrden).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.ProveedorNombreSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ProveedorDocumentoSnapshot).HasMaxLength(120);
        builder.Property(x => x.Moneda).HasMaxLength(3).IsRequired();
        builder.Property(x => x.CondicionesCompra).HasMaxLength(1000);
        builder.Property(x => x.Observaciones).HasMaxLength(1000);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.IdempotencyFingerprint).HasMaxLength(64);
        builder.Property(x => x.AprobadaPorNombreSnapshot).HasMaxLength(150);
        builder.Property(x => x.MotivoCancelacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.NumeroOrden)
            .IsUnique()
            .HasDatabaseName("UX_OrdenesCompra_NumeroOrden");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_OrdenesCompra_IdempotencyKey");

        builder.HasIndex(x => new { x.Estado, x.FechaEsperadaUtc })
            .HasDatabaseName("IX_OrdenesCompra_Estado_FechaEsperada");

        builder.HasIndex(x => x.ProveedorId)
            .HasDatabaseName("IX_OrdenesCompra_ProveedorId");

        builder.HasIndex(x => x.SolicitudCompraId)
            .HasDatabaseName("IX_OrdenesCompra_SolicitudCompraId");

        builder.HasOne(x => x.Proveedor)
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_OrdenesCompra_Proveedores_ProveedorId");

        builder.HasOne(x => x.SolicitudCompra)
            .WithMany()
            .HasForeignKey(x => x.SolicitudCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_OrdenesCompra_SolicitudesCompra_SolicitudCompraId");

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.OrdenCompra)
            .HasForeignKey(x => x.OrdenCompraId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_OrdenCompraDetalles_OrdenesCompra_OrdenCompraId");
    }
}
