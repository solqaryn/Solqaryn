using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("RolPermisos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RolId).IsRequired();
        builder.Property(p => p.PermisoId).IsRequired();

        builder.HasOne(p => p.RolEntidad)
            .WithMany(r => r.Permisos)
            .HasForeignKey(p => p.RolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Permiso)
            .WithMany(p => p.Asignaciones)
            .HasForeignKey(p => p.PermisoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.RolId, p.PermisoId }).IsUnique();
    }
}
