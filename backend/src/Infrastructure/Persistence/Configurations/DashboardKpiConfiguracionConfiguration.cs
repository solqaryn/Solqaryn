using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class DashboardKpiConfiguracionConfiguration : IEntityTypeConfiguration<DashboardKpiConfiguracion>
{
    public void Configure(EntityTypeBuilder<DashboardKpiConfiguracion> builder)
    {
        builder.ToTable("DashboardKpiConfiguraciones", table =>
        {
            table.HasCheckConstraint(
                "CK_DashboardKpiConfiguraciones_Owner",
                "(`UsuarioId` IS NOT NULL AND `RolId` IS NULL) OR (`UsuarioId` IS NULL AND `RolId` IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetricKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Habilitado)
            .HasDefaultValue(true);

        builder.Property(x => x.Orden)
            .HasDefaultValue(0);

        builder.Property(x => x.EtiquetaVisible)
            .HasMaxLength(150);

        // MySQL unique indexes allow NULLs. Because the owner check requires
        // exactly one owner, the corresponding unique index prevents duplicate
        // metric configuration for that user or role while the other index is
        // intentionally non-applicable to the row.
        builder.HasIndex(x => new { x.UsuarioId, x.MetricKey })
            .IsUnique()
            .HasDatabaseName("UX_DashboardKpiConfiguraciones_Usuario_MetricKey");

        builder.HasIndex(x => new { x.RolId, x.MetricKey })
            .IsUnique()
            .HasDatabaseName("UX_DashboardKpiConfiguraciones_Rol_MetricKey");

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Rol)
            .WithMany()
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
