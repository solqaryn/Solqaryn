using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class UsuarioRolPlataformaConfiguration : IEntityTypeConfiguration<UsuarioRolPlataforma>
{
    public void Configure(EntityTypeBuilder<UsuarioRolPlataforma> builder)
    {
        builder.ToTable("UsuarioRolesPlataforma", table =>
        {
            table.HasCheckConstraint("CK_UsuarioRolesPlataforma_UsuarioId", "`UsuarioId` > 0");
            table.HasCheckConstraint("CK_UsuarioRolesPlataforma_RolId", "`RolId` > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.UsuarioId).IsRequired();
        builder.Property(x => x.RolId).IsRequired();
        builder.Property(x => x.Activa).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.UsuarioId, x.RolId })
            .IsUnique()
            .HasDatabaseName("UX_UsuarioRolesPlataforma_UsuarioId_RolId");
        builder.HasIndex(x => new { x.UsuarioId, x.Activa })
            .HasDatabaseName("IX_UsuarioRolesPlataforma_UsuarioId_Activa");
        builder.HasIndex(x => x.RolId)
            .HasDatabaseName("IX_UsuarioRolesPlataforma_RolId");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Rol>()
            .WithMany()
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
