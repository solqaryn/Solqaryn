using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Entities.Cajas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.1.C — persistencia grounded de la sesión de caja.
/// Conserva Apertura→Operaciones→Arqueo→Cierre y los acumulados monetarios del dominio.
/// </summary>
public sealed class CajaSesionConfiguration : IEntityTypeConfiguration<CajaSesion>
{
    public void Configure(EntityTypeBuilder<CajaSesion> builder)
    {
        builder.ToTable("CajaSesiones", table =>
        {
            table.HasCheckConstraint("CK_CajaSesiones_CajaId", "`CajaId` > 0");
            table.HasCheckConstraint("CK_CajaSesiones_UsuarioId", "`UsuarioId` > 0");
            table.HasCheckConstraint("CK_CajaSesiones_Estado", "`Estado` IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_CajaSesiones_FondoInicial", "`FondoInicial` >= 0");
            table.HasCheckConstraint("CK_CajaSesiones_TotalIngresos", "`TotalIngresos` >= 0");
            table.HasCheckConstraint("CK_CajaSesiones_TotalRetiros", "`TotalRetiros` >= 0");
            table.HasCheckConstraint("CK_CajaSesiones_TotalDepositos", "`TotalDepositos` >= 0");
            table.HasCheckConstraint(
                "CK_CajaSesiones_SaldoContado",
                "`SaldoContado` IS NULL OR `SaldoContado` >= 0");
            table.HasCheckConstraint(
                "CK_CajaSesiones_FechaCierre",
                "(`Estado` = 4 AND `FechaCierre` IS NOT NULL) OR (`Estado` <> 4 AND `FechaCierre` IS NULL)");
        });

        builder.Property(x => x.FondoInicial).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.TotalIngresos).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.TotalRetiros).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.TotalDepositos).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.SaldoEsperado).HasColumnType("decimal(18,4)");
        builder.Property(x => x.SaldoContado).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Diferencia).HasColumnType("decimal(18,4)");
        builder.Property(x => x.FechaApertura).IsRequired();
        builder.Property(x => x.Estado).IsRequired();

        builder.HasIndex(x => x.CajaId)
            .HasDatabaseName("IX_CajaSesiones_CajaId");
        builder.HasIndex(x => x.UsuarioId)
            .HasDatabaseName("IX_CajaSesiones_UsuarioId");
        builder.HasIndex(x => new { x.CajaId, x.Estado })
            .HasDatabaseName("IX_CajaSesiones_CajaId_Estado");

        builder.HasOne<Caja>()
            .WithMany()
            .HasForeignKey(x => x.CajaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Movimientos)
            .WithOne()
            .HasForeignKey(x => x.CajaSesionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Movimientos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
