using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");
        builder.Property(v => v.NumeroVenta).IsRequired().HasMaxLength(20);
        builder.HasIndex(v => v.NumeroVenta).IsUnique();
        builder.Property(v => v.ClienteNombre).IsRequired().HasMaxLength(200);
        builder.Property(v => v.ClienteTelefono).HasMaxLength(30);
        builder.Property(v => v.ClienteIdentidadORTN).HasMaxLength(50);
        builder.Property(v => v.ClienteCorreo).HasMaxLength(150);
        builder.Property(v => v.ClienteDireccion).HasMaxLength(300);
        builder.Property(v => v.Notas).HasMaxLength(1000);
        builder.Property(v => v.MotivoAnulacion).HasMaxLength(500);
        builder.Property(v => v.CostoEnvioNombreSnapshot).HasMaxLength(150);
        builder.Property(v => v.CostoEnvioDepartamentoSnapshot).HasMaxLength(120);
        builder.Property(v => v.CostoEnvioCiudadSnapshot).HasMaxLength(120);
        builder.Property(v => v.CostoEnvioZonaSnapshot).HasMaxLength(150);
        builder.Property(v => v.CostoEnvioModalidadSnapshot).HasMaxLength(80);
        builder.Property(v => v.MotivoExoneracionEnvio).HasMaxLength(500);

        builder.Property(v => v.ImporteBruto).HasColumnType("decimal(18,2)");
        builder.Property(v => v.ImporteProductos).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Descuento).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Impuesto).HasColumnType("decimal(18,2)");
        builder.Property(v => v.CostoEnvio).HasColumnType("decimal(18,2)");
        builder.Property(v => v.CostoEnvioMontoSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Total).HasColumnType("decimal(18,2)");
        builder.Property(v => v.CostoTotal).HasColumnType("decimal(18,2)");
        builder.Property(v => v.UtilidadBruta).HasColumnType("decimal(18,2)");

        builder.Property(v => v.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.EstadoPago).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.MetodoPago).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Eliminado).HasDefaultValue(false);
        builder.HasIndex(v => v.Eliminado);
        builder.HasIndex(v => v.Fecha)
            .HasDatabaseName("IX_Ventas_Fecha");
        builder.HasIndex(v => v.CreadoPorUsuarioId)
            .HasDatabaseName("IX_Ventas_CreadoPorUsuarioId");
        builder.HasIndex(v => v.MetodoPagoId)
            .HasDatabaseName("IX_Ventas_MetodoPagoId");
        builder.HasQueryFilter(v => !v.Eliminado);

        builder.HasOne(v => v.MetodoPagoCatalogo)
            .WithMany()
            .HasForeignKey(v => v.MetodoPagoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Detalles)
            .WithOne(d => d.Venta)
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.DescuentosAplicados)
            .WithOne()
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.ImpuestosAplicados)
            .WithOne()
            .HasForeignKey(i => i.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Venta y Factura son documentos históricos. Una eliminación física
        // accidental de la venta nunca debe propagar la eliminación de factura.
        builder.HasOne(v => v.Factura)
            .WithOne(f => f.Venta)
            .HasForeignKey<Factura>(f => f.VentaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
