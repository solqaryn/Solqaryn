using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class TipoClienteConfiguration : IEntityTypeConfiguration<TipoCliente>
{
    public void Configure(EntityTypeBuilder<TipoCliente> builder)
    {
        builder.ToTable("TipoClientes");

        builder.Property(tc => tc.Codigo)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(tc => tc.Codigo)
            .IsUnique();

        builder.Property(tc => tc.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tc => tc.NombreNormalizado)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(tc => tc.NombreNormalizado)
            .IsUnique();

        builder.Property(tc => tc.Descripcion)
            .HasMaxLength(500);

        builder.Property(tc => tc.ColorHex)
            .IsRequired()
            .HasMaxLength(7);

        builder.HasMany(tc => tc.Clientes)
            .WithOne(c => c.TipoCliente)
            .HasForeignKey(c => c.TipoClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(tc => tc.EsPredeterminadoUnico)
            .HasMaxLength(10)
            .HasComputedColumnSql("IF(EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0, 'DEFAULT', NULL)", stored: true);

        builder.HasIndex(tc => tc.EsPredeterminadoUnico)
            .IsUnique();

        builder.HasQueryFilter(tc => !tc.Eliminado);
    }
}
