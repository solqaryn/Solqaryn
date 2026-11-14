using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N6.4.C — persistencia tenant-aware de membresías Usuario↔Empresa.
/// La unicidad física se limita a (UsuarioId, EmpresaId); el RolId permanece
/// mutable dentro de esa membresía y no forma parte de la identidad física.
/// </summary>
public sealed class UsuarioEmpresaConfiguration : IEntityTypeConfiguration<UsuarioEmpresa>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresa> builder)
    {
        builder.ToTable("UsuarioEmpresas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UsuarioId).IsRequired();
        builder.Property(x => x.EmpresaId).IsRequired();
        builder.Property(x => x.RolId).IsRequired();
        builder.Property(x => x.Activa).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.UsuarioId, x.EmpresaId })
            .IsUnique()
            .HasDatabaseName("UX_UsuarioEmpresas_UsuarioId_EmpresaId");

        builder.HasIndex(x => new { x.EmpresaId, x.Activa })
            .HasDatabaseName("IX_UsuarioEmpresas_EmpresaId_Activa");

        builder.HasIndex(x => x.RolId)
            .HasDatabaseName("IX_UsuarioEmpresas_RolId");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Rol>()
            .WithMany()
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
