using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class AjusteInventarioConfiguration : IEntityTypeConfiguration<AjusteInventario>
{
    public void Configure(EntityTypeBuilder<AjusteInventario> builder)
    {
        builder.ToTable("AjustesInventario");
        builder.Property(a => a.NumeroAjuste).IsRequired().HasMaxLength(20);
        builder.HasIndex(a => a.NumeroAjuste)
            .IsUnique()
            .HasDatabaseName("UX_AjustesInventario_Numero");
        builder.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Motivo).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Observaciones).HasMaxLength(1000);
        builder.Property(a => a.MotivoAnulacion).HasMaxLength(500);
        builder.Property(a => a.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(a => a.ActualizadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(a => a.ConfirmadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(a => a.AnuladoPorNombreUsuario).HasMaxLength(150);
        builder.HasIndex(a => new { a.Estado, a.FechaAjuste })
            .HasDatabaseName("IX_AjustesInventario_Estado_Fecha");

        builder.HasMany(a => a.Detalles)
            .WithOne(d => d.AjusteInventario)
            .HasForeignKey(d => d.AjusteInventarioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AjusteInventarioDetalles_AjustesInventario");
    }
}
