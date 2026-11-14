using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica de la topología interna de almacenes para ERP-N1.3.
/// No modela existencias ni duplica contexto de Sucursal/Empresa.
/// </summary>
public sealed class UbicacionAlmacenConfiguration : IEntityTypeConfiguration<UbicacionAlmacen>
{
    public void Configure(EntityTypeBuilder<UbicacionAlmacen> builder)
    {
        builder.ToTable("UbicacionesAlmacen");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.AlmacenId).IsRequired();
        builder.Property(x => x.UbicacionPadreId);
        builder.Property(x => x.Codigo).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Tipo).HasConversion<int>().IsRequired();
        builder.Property(x => x.Activa).HasDefaultValue(true);
        builder.Property(x => x.Eliminado).HasDefaultValue(false);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<string>("CodigoActivoUnico")
            .HasMaxLength(60)
            .HasComputedColumnSql(
                "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)",
                stored: true);

        builder.HasQueryFilter(x => !x.Eliminado);

        // Clave alterna necesaria para la FK jerárquica compuesta: demuestra
        // físicamente que padre e hijo pertenecen al mismo Almacén.
        builder.HasAlternateKey(x => new { x.AlmacenId, x.Id })
            .HasName("AK_UbicacionesAlmacen_AlmacenId_Id");

        builder.HasIndex(new[] { "AlmacenId", "CodigoActivoUnico" })
            .IsUnique()
            .HasDatabaseName("UX_UbicacionesAlmacen_Almacen_Codigo_Activo");
        builder.HasIndex(x => x.AlmacenId)
            .HasDatabaseName("IX_UbicacionesAlmacen_AlmacenId");
        builder.HasIndex(x => new { x.AlmacenId, x.UbicacionPadreId })
            .HasDatabaseName("IX_UbicacionesAlmacen_Padre");
        builder.HasIndex(x => new { x.Tipo, x.Activa, x.Eliminado })
            .HasDatabaseName("IX_UbicacionesAlmacen_Tipo_Estado");

        builder.HasOne(x => x.Almacen)
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired()
            .HasConstraintName("FK_UbicacionesAlmacen_Almacenes_AlmacenId");

        builder.HasOne(x => x.UbicacionPadre)
            .WithMany(x => x.Hijas)
            .HasForeignKey(x => new { x.AlmacenId, x.UbicacionPadreId })
            .HasPrincipalKey(x => new { x.AlmacenId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_UbicacionesAlmacen_Padre_MismoAlmacen");
    }
}
