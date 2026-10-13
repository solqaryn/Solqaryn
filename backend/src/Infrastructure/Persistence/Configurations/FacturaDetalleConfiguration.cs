using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class FacturaDetalleConfiguration : IEntityTypeConfiguration<FacturaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaDetalle> builder)
    {
        builder.ToTable("FacturaDetalles");
        builder.HasIndex(x => new { x.EmpresaId, x.FacturaId })
            .HasDatabaseName("IX_FacturaDetalles_Empresa_Factura");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FacturaDetalles_Empresas_EmpresaId");
        builder.Property(d => d.ProductoNombre).IsRequired().HasMaxLength(150);
        builder.Property(d => d.ProductoMarca).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoModelo).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ProductoCodigo).HasMaxLength(50);
        builder.Property(d => d.VarianteColor).HasMaxLength(100);
        builder.Property(d => d.VarianteTalla).HasMaxLength(100);
        builder.Property(d => d.VarianteSku).HasMaxLength(80);
        builder.Property(d => d.PrecioUnitario).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Descuento).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");
    }
}
