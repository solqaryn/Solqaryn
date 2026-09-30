using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class EvaluacionProveedorConfiguration : IEntityTypeConfiguration<EvaluacionProveedor>
{
    public void Configure(EntityTypeBuilder<EvaluacionProveedor> builder)
    {
        builder.ToTable("EvaluacionesProveedor", table =>
        {
            table.HasCheckConstraint("CK_EvaluacionesProveedor_ProveedorId_Valido", "`ProveedorId` > 0");
            table.HasCheckConstraint("CK_EvaluacionesProveedor_OrdenCompraId_Valido", "`OrdenCompraId` > 0");
            table.HasCheckConstraint("CK_EvaluacionesProveedor_RecepcionCompraId_Valido", "`RecepcionCompraId` > 0");
            table.HasCheckConstraint("CK_EvaluacionesProveedor_CantidadOrdenada_NoNegativa", "`CantidadOrdenada` >= 0");
            table.HasCheckConstraint("CK_EvaluacionesProveedor_CantidadAceptada_NoNegativa", "`CantidadAceptada` >= 0");
            table.HasCheckConstraint("CK_EvaluacionesProveedor_CantidadDanada_NoNegativa", "`CantidadDanada` >= 0");
            table.HasCheckConstraint("CK_EvaluacionesProveedor_CantidadSobrante_NoNegativa", "`CantidadSobrante` >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CantidadOrdenada).HasPrecision(18, 4);
        builder.Property(x => x.CantidadAceptada).HasPrecision(18, 4);
        builder.Property(x => x.CantidadDanada).HasPrecision(18, 4);
        builder.Property(x => x.CantidadSobrante).HasPrecision(18, 4);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.RecepcionCompraId)
            .HasDatabaseName("IX_EvaluacionesProveedor_RecepcionCompra");

        builder.HasIndex(x => x.OrdenCompraId)
            .HasDatabaseName("IX_EvaluacionesProveedor_OrdenCompra");

        builder.HasIndex(x => new { x.ProveedorId, x.FechaRecepcionUtc })
            .HasDatabaseName("IX_EvaluacionesProveedor_Proveedor_FechaRecepcion");

        builder.HasOne(x => x.Proveedor)
            .WithMany()
            .HasForeignKey(x => x.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EvaluacionesProveedor_Proveedores_ProveedorId");

        builder.HasOne(x => x.OrdenCompra)
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EvaluacionesProveedor_OrdenesCompra_OrdenCompraId");

        builder.HasOne(x => x.RecepcionCompra)
            .WithMany()
            .HasForeignKey(x => x.RecepcionCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EvaluacionesProveedor_RecepcionesCompra_RecepcionCompraId");
    }
}
