using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles", table =>
        {
            table.HasCheckConstraint("CK_Roles_Ambito", "`Ambito` IN (1, 2)");
            table.HasCheckConstraint(
                "CK_Roles_Ambito_Empresa",
                "(`Ambito` = 2 AND `EmpresaId` IS NULL) OR (`Ambito` = 1)");
            table.HasCheckConstraint(
                "CK_Roles_EmpresaId_Positivo",
                "`EmpresaId` IS NULL OR `EmpresaId` > 0");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Nombre).IsRequired().HasMaxLength(80);
        builder.Property(r => r.NombreNormalizado).IsRequired().HasMaxLength(80);
        builder.Property(r => r.Descripcion).HasMaxLength(300);
        builder.Property(r => r.Ambito)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(AmbitoAutorizacion.Empresa);
        builder.Property(r => r.EmpresaId).IsRequired(false);

        builder.HasIndex(r => r.NombreNormalizado).IsUnique();
        builder.HasIndex(r => r.EmpresaId).HasDatabaseName("IX_Roles_EmpresaId");
        builder.HasIndex(r => new { r.Ambito, r.Activo })
            .HasDatabaseName("IX_Roles_Ambito_Activo");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(r => r.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Usuarios)
            .WithOne(u => u.RolEntidad)
            .HasForeignKey(u => u.RolId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Permisos)
            .WithOne(p => p.RolEntidad)
            .HasForeignKey(p => p.RolId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
