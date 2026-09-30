using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica del dictamen three-way match ERP-N2.5.
/// Conserva el resultado y sus discrepancias como evidencia auditable sin inventar
/// tolerancias, CxP, FX ni relaciones incompatibles con el centinela de cabecera.
/// </summary>
public sealed class ThreeWayMatchResultConfiguration : IEntityTypeConfiguration<ThreeWayMatchResult>
{
    public void Configure(EntityTypeBuilder<ThreeWayMatchResult> builder)
    {
        builder.ToTable("ThreeWayMatchResultados", table =>
        {
            table.HasCheckConstraint("CK_ThreeWayMatchResultados_OrdenCompraValida", "OrdenCompraId > 0");
            table.HasCheckConstraint("CK_ThreeWayMatchResultados_EstadoValido", "Estado IN (0, 1, 2)");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.OrdenCompraId)
            .HasDatabaseName("IX_ThreeWayMatchResultados_OrdenCompraId");

        builder.HasIndex(x => new { x.OrdenCompraId, x.FechaCreacion })
            .HasDatabaseName("IX_ThreeWayMatchResultados_OrdenCompra_Fecha");

        builder.HasOne<OrdenCompra>()
            .WithMany()
            .HasForeignKey(x => x.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ThreeWayMatchResultados_OrdenesCompra_OrdenCompraId");

        builder.OwnsMany(x => x.Discrepancias, discrepancy =>
        {
            discrepancy.ToTable("ThreeWayMatchDiscrepancias", table =>
            {
                table.HasCheckConstraint(
                    "CK_ThreeWayMatchDiscrepancias_OrdenDetalleSentinela",
                    "OrdenCompraDetalleId >= 0");
                table.HasCheckConstraint(
                    "CK_ThreeWayMatchDiscrepancias_TipoValido",
                    "Tipo IN (1, 2, 3, 4, 5)");
            });

            discrepancy.WithOwner()
                .HasForeignKey("ThreeWayMatchResultId");

            discrepancy.Property<int>("Id")
                .ValueGeneratedOnAdd();

            discrepancy.HasKey("Id");

            discrepancy.Property(x => x.OrdenCompraDetalleId).IsRequired();
            discrepancy.Property(x => x.Tipo).HasConversion<int>().IsRequired();
            discrepancy.Property(x => x.EsperadoOrdenado).HasPrecision(18, 4);
            discrepancy.Property(x => x.ValorRecepcion).HasPrecision(18, 4);
            discrepancy.Property(x => x.ValorFacturado).HasPrecision(18, 4);
            discrepancy.Property(x => x.Mensaje).HasMaxLength(500).IsRequired();
            discrepancy.Property(x => x.EsperadoTexto).HasMaxLength(500);
            discrepancy.Property(x => x.ValorFacturadoTexto).HasMaxLength(500);

            discrepancy.HasIndex("ThreeWayMatchResultId")
                .HasDatabaseName("IX_ThreeWayMatchDiscrepancias_ResultId");
            discrepancy.HasIndex(x => x.OrdenCompraDetalleId)
                .HasDatabaseName("IX_ThreeWayMatchDiscrepancias_OrdenDetalleId");

            // N2.5.B usa OrdenCompraDetalleId=0 para discrepancias de cabecera (moneda).
            // Por contrato NO se crea FK física a OrdenCompraDetalles.
        });

        builder.Navigation(x => x.Discrepancias)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
