using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MetodoPagoCatalogo = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class MetodoPagoConfiguration : IEntityTypeConfiguration<MetodoPagoCatalogo>
{
    public void Configure(EntityTypeBuilder<MetodoPagoCatalogo> builder)
    {
        builder.ToTable("MetodosPago");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.CodigoNormalizado)
            .HasMaxLength(50)
            .HasComputedColumnSql("LOWER(TRIM(Codigo))", stored: true);
        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(120);
        builder.Property(x => x.Tipo)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.Metadata)
            .HasColumnType("json");

        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.RequiereReferencia).HasDefaultValue(false);
        builder.Property(x => x.RequiereBanco).HasDefaultValue(false);
        builder.Property(x => x.PermiteCambio).HasDefaultValue(false);
        builder.Property(x => x.Orden).HasDefaultValue(0);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);

        // Codigo es autoridad funcional estable y no se puede reutilizar aunque
        // el registro sea desactivado o eliminado lógicamente.
        builder.HasIndex(x => x.CodigoNormalizado)
            .IsUnique()
            .HasDatabaseName("UX_MetodosPago_Codigo_Normalizado");
        builder.HasIndex(x => x.Nombre)
            .HasDatabaseName("IX_MetodosPago_Nombre");
        builder.HasIndex(x => new { x.Activo, x.Eliminado, x.Orden })
            .HasDatabaseName("IX_MetodosPago_Estado_Orden");

        builder.HasQueryFilter(x => !x.Eliminado);
    }
}
