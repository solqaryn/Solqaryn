using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class MovimientoFinancieroConfiguration : IEntityTypeConfiguration<MovimientoFinanciero>
{
    public void Configure(EntityTypeBuilder<MovimientoFinanciero> builder)
    {
        builder.ToTable("MovimientosFinancieros");
        builder.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Categoria).HasConversion<string>().HasMaxLength(30);
        builder.Property(m => m.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.MetodoPago).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Concepto).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Descripcion).HasMaxLength(500);
        // ModuloOrigen/ReferenciaId se conservan como snapshot de auditoría y
        // correlación. Las FKs tipadas de abajo son la autoridad relacional.
        builder.Property(m => m.ModuloOrigen).IsRequired().HasMaxLength(30);
        builder.Property(m => m.MotivoAnulacion).HasMaxLength(500);
        builder.Property(m => m.Monto).HasColumnType("decimal(18,2)");

        builder.HasIndex(m => m.CompraId);
        builder.HasIndex(m => m.VentaId);
        builder.HasIndex(m => m.FacturaId);
        builder.HasIndex(m => m.MetodoPagoId)
            .HasDatabaseName("IX_MovimientosFinancieros_MetodoPagoId");
        builder.HasIndex(m => new { m.ModuloOrigen, m.ReferenciaId });
        builder.HasIndex(m => new { m.Estado, m.Fecha });

        builder.HasOne(m => m.MetodoPagoCatalogo)
            .WithMany()
            .HasForeignKey(m => m.MetodoPagoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Compra>()
            .WithMany()
            .HasForeignKey(m => m.CompraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(m => m.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Factura>()
            .WithMany()
            .HasForeignKey(m => m.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
