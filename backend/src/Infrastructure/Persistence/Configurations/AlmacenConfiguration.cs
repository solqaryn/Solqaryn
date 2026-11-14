using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de almacenes para ERP-N1.2. Almacén depende de Sucursal;
/// el contexto multiempresa futuro se deriva de esa relación y no se duplica aquí.
/// </summary>
public sealed class AlmacenConfiguration : IEntityTypeConfiguration<Almacen>
{
    public void Configure(EntityTypeBuilder<Almacen> builder)
    {
        builder.ToTable("Almacenes");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.SucursalId).IsRequired();
        builder.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Tipo).HasConversion<int>().IsRequired();
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
            .HasDatabaseName("UX_Almacenes_Codigo_Activo");
        builder.HasIndex(x => x.SucursalId)
            .HasDatabaseName("IX_Almacenes_SucursalId");
        builder.HasIndex(x => new { x.Tipo, x.Activo, x.Eliminado })
            .HasDatabaseName("IX_Almacenes_Tipo_Estado");

        builder.HasOne(x => x.Sucursal)
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired()
            .HasConstraintName("FK_Almacenes_Sucursales_SucursalId");
    }
}
