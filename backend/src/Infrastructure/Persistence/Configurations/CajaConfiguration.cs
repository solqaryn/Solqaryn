using Solqaryn.Domain.Entities.Cajas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.1.C — persistencia grounded de la caja física.
/// Mantiene el lifecycle y el puntero de sesión activa definidos por el dominio
/// sin introducir políticas contables, POS o de conciliación no certificadas.
/// </summary>
public sealed class CajaConfiguration : IEntityTypeConfiguration<Caja>
{
    public void Configure(EntityTypeBuilder<Caja> builder)
    {
        builder.ToTable("Cajas", table =>
        {
            table.HasCheckConstraint("CK_Cajas_Estado", "`Estado` IN (1, 2)");
            table.HasCheckConstraint(
                "CK_Cajas_SesionActivaId",
                "`SesionActivaId` IS NULL OR `SesionActivaId` > 0");
        });

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Estado)
            .IsRequired();

        builder.HasIndex(x => x.Estado)
            .HasDatabaseName("IX_Cajas_Estado");

        builder.HasIndex(x => x.SesionActivaId)
            .IsUnique()
            .HasDatabaseName("UX_Cajas_SesionActivaId");

        builder.HasOne<CajaSesion>()
            .WithMany()
            .HasForeignKey(x => x.SesionActivaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
