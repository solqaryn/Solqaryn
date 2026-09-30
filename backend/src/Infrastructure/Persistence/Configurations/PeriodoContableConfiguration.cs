using Solqaryn.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class PeriodoContableConfiguration : IEntityTypeConfiguration<PeriodoContable>
{
    public void Configure(EntityTypeBuilder<PeriodoContable> builder)
    {
        builder.ToTable("PeriodosContables", table =>
        {
            table.HasCheckConstraint("CK_PeriodosContables_Rango", "`FechaFin` >= `FechaInicio`");
            table.HasCheckConstraint("CK_PeriodosContables_Estado", "`Estado` IN (1, 2)");
            table.HasCheckConstraint("CK_PeriodosContables_Cierre", "(`Estado` = 1 AND `CerradoEnUtc` IS NULL) OR (`Estado` = 2 AND `CerradoEnUtc` IS NOT NULL)");
        });

        builder.Property(x => x.FechaInicio).IsRequired();
        builder.Property(x => x.FechaFin).IsRequired();
        builder.Property(x => x.Estado).IsRequired();

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.FechaInicio, x.FechaFin })
            .IsUnique()
            .HasDatabaseName("UX_PeriodosContables_Rango");

        builder.HasIndex(x => new { x.Estado, x.FechaInicio, x.FechaFin })
            .HasDatabaseName("IX_PeriodosContables_Estado_Rango");
    }
}
