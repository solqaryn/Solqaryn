using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class CotizacionConfiguration : IEntityTypeConfiguration<Cotizacion>
{
    public void Configure(EntityTypeBuilder<Cotizacion> builder)
    {
        builder.ToTable("Cotizaciones", table =>
        {
            table.HasCheckConstraint(
                "CK_Cotizaciones_Estado",
                $"`Estado` IN ({(int)EstadoCotizacion.Borrador}, {(int)EstadoCotizacion.Enviada}, {(int)EstadoCotizacion.Aceptada}, {(int)EstadoCotizacion.Rechazada}, {(int)EstadoCotizacion.Convertida})");
        });

        builder.Property(c => c.Estado).HasConversion<int>().IsRequired();
        builder.Property(c => c.ClienteNombreSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ClienteDocumentoSnapshot).HasMaxLength(50);
        builder.Property(c => c.Observaciones).HasMaxLength(1000);
        builder.Property(c => c.MotivoRechazo).HasMaxLength(500);

        builder.HasIndex(c => c.ClienteId).HasDatabaseName("IX_Cotizaciones_ClienteId");
        builder.HasIndex(c => c.Estado).HasDatabaseName("IX_Cotizaciones_Estado");

        builder.HasOne(c => c.Cliente)
            .WithMany()
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Detalles)
            .WithOne(d => d.Cotizacion)
            .HasForeignKey(d => d.CotizacionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
