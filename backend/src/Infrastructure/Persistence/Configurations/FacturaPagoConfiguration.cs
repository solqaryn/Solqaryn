using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class FacturaPagoConfiguration : IEntityTypeConfiguration<FacturaPago>
{
    public void Configure(EntityTypeBuilder<FacturaPago> builder)
    {
        builder.ToTable("FacturaPagos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Monto).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.MontoRecibido).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Cambio).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Referencia).HasMaxLength(120);
        builder.Property(x => x.MetodoPagoCodigoSnapshot).HasMaxLength(50);
        builder.Property(x => x.MetodoPagoNombreSnapshot).HasMaxLength(120);
        builder.Property(x => x.BancoCodigoSnapshot).HasMaxLength(50);
        builder.Property(x => x.BancoNombreSnapshot).HasMaxLength(120);
        builder.Property(x => x.Observaciones).HasMaxLength(500);
        builder.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        builder.HasIndex(x => new { x.FacturaId, x.FechaPago });
        builder.HasIndex(x => x.MetodoPagoId)
            .HasDatabaseName("IX_FacturaPagos_MetodoPagoId");
        builder.HasIndex(x => x.BancoId)
            .HasDatabaseName("IX_FacturaPagos_BancoId");

        builder.HasOne(x => x.MetodoPagoCatalogo)
            .WithMany()
            .HasForeignKey(x => x.MetodoPagoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Banco)
            .WithMany()
            .HasForeignKey(x => x.BancoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Factura)
            .WithMany(x => x.Pagos)
            .HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
