using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.NombreUsuario).IsUnique();
        builder.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(300);
        builder.Property(u => u.RolId).IsRequired();
        builder.Ignore(u => u.Rol);
        builder.Property(u => u.MotivoBloqueo).HasMaxLength(300);
        builder.Property(u => u.FotoPerfilUrl).HasMaxLength(500);
        builder.Property(u => u.FotoPerfilPublicId).HasMaxLength(255);
        builder.HasIndex(u => u.RolId);
        builder.HasIndex(u => u.Eliminado);

        // ERP-N0.4 retira el usuario seed del modelo EF para no mantener un rol
        // enum persistido. SeedAdmin/SeedPermisoService crean datos mediante RolId.
    }
}
