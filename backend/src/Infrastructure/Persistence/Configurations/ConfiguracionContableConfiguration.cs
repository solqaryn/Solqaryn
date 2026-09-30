using Solqaryn.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionContableConfiguration : IEntityTypeConfiguration<ConfiguracionContable>
{
    public void Configure(EntityTypeBuilder<ConfiguracionContable> builder)
    {
        builder.ToTable("ConfiguracionesContables");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Evento).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.Activo).HasDefaultValue(true);

        builder.HasIndex(x => x.Evento)
            .IsUnique()
            .HasDatabaseName("UX_ConfiguracionesContables_Evento");

        builder.HasOne(x => x.CuentaDebe)
            .WithMany()
            .HasForeignKey(x => x.CuentaDebeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CuentaHaber)
            .WithMany()
            .HasForeignKey(x => x.CuentaHaberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
