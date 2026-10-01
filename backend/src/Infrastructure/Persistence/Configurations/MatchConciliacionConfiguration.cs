using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Entities.Bancos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class MatchConciliacionConfiguration : IEntityTypeConfiguration<MatchConciliacion>
{
    public void Configure(EntityTypeBuilder<MatchConciliacion> builder)
    {
        builder.ToTable("MatchesConciliacion", table =>
        {
            table.HasCheckConstraint("CK_MatchesConciliacion_MovimientoEstadoCuentaId", "`MovimientoEstadoCuentaId` > 0");
            table.HasCheckConstraint("CK_MatchesConciliacion_MovimientoFinancieroId", "`MovimientoFinancieroId` > 0");
            table.HasCheckConstraint("CK_MatchesConciliacion_MontoAplicado", "`MontoAplicado` > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MovimientoEstadoCuentaId).IsRequired();
        builder.Property(x => x.MovimientoFinancieroId).IsRequired();
        builder.Property(x => x.MontoAplicado).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TipoMatch).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasOne(x => x.MovimientoEstadoCuenta)
            .WithMany(x => x.Matches)
            .HasForeignKey(x => x.MovimientoEstadoCuentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MovimientoFinanciero>()
            .WithMany()
            .HasForeignKey(x => x.MovimientoFinancieroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MovimientoEstadoCuentaId, x.MovimientoFinancieroId })
            .IsUnique()
            .HasDatabaseName("UX_MatchesConciliacion_MovimientoEstadoCuenta_MovimientoFinanciero");
        builder.HasIndex(x => x.MovimientoFinancieroId)
            .HasDatabaseName("IX_MatchesConciliacion_MovimientoFinancieroId");
    }
}
