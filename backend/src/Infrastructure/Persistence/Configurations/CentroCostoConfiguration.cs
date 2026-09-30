using Solqaryn.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N4.11.C — persistencia canónica de centros de costo.
/// Solo Sucursal tiene una relación estructural real; los demás tipos permanecen
/// como categorías sin FKs ficticias hasta que existan sus agregados de dominio.
/// </summary>
public sealed class CentroCostoConfiguration : IEntityTypeConfiguration<CentroCosto>
{
    public void Configure(EntityTypeBuilder<CentroCosto> builder)
    {
        builder.ToTable("CentrosCosto", table =>
        {
            table.HasCheckConstraint(
                "CK_CentrosCosto_Tipo",
                "`Tipo` IN (1, 2, 3, 4)");
            table.HasCheckConstraint(
                "CK_CentrosCosto_Asociacion",
                "(`Tipo` = 1 AND `SucursalId` IS NOT NULL) OR (`Tipo` <> 1 AND `SucursalId` IS NULL)");
        });

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.Tipo).IsRequired();
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<string>("CodigoActivoUnico")
            .HasMaxLength(40)
            .HasComputedColumnSql(
                "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)",
                stored: true);

        builder.HasQueryFilter(x => !x.Eliminado);
        builder.HasIndex("CodigoActivoUnico")
            .IsUnique()
            .HasDatabaseName("UX_CentrosCosto_Codigo_Activo");
        builder.HasIndex(x => new { x.Tipo, x.Activo, x.Eliminado })
            .HasDatabaseName("IX_CentrosCosto_Tipo_Estado");
        builder.HasIndex(x => x.SucursalId)
            .HasDatabaseName("IX_CentrosCosto_SucursalId");

        builder.HasOne(x => x.Sucursal)
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
