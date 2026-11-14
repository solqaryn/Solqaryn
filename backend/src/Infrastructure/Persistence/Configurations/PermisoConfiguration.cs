using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("Permisos", table =>
        {
            table.HasCheckConstraint("CK_Permisos_Ambito", "`Ambito` IN (1, 2)");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Codigo).IsRequired().HasMaxLength(120);
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Descripcion).HasMaxLength(300);
        builder.Property(p => p.Ambito)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(AmbitoAutorizacion.Empresa);

        builder.HasIndex(p => p.Codigo).IsUnique();
        builder.HasIndex(p => new { p.Modulo, p.Accion }).IsUnique();
        builder.HasIndex(p => new { p.Ambito, p.Activo })
            .HasDatabaseName("IX_Permisos_Ambito_Activo");

        builder.Navigation(p => p.Asignaciones).AutoInclude(false);
    }
}
