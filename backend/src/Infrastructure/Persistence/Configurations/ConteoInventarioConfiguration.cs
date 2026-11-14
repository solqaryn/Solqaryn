using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistencia canónica del documento de conteo físico ERP-N1.7.
/// El almacén define el scope físico; ubicación y categoría son filtros opcionales.
/// </summary>
public sealed class ConteoInventarioConfiguration : IEntityTypeConfiguration<ConteoInventario>
{
    public void Configure(EntityTypeBuilder<ConteoInventario> builder)
    {
        builder.ToTable("ConteosInventario");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Numero).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Tipo).HasConversion<int>().IsRequired();
        builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
        builder.Property(x => x.EsCiego).HasDefaultValue(false);
        builder.Property(x => x.Observaciones).HasMaxLength(1000);
        builder.Property(x => x.MotivoCancelacion).HasMaxLength(500);
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasAlternateKey(x => new { x.Id, x.AlmacenId })
            .HasName("AK_ConteosInventario_Id_AlmacenId");

        builder.HasIndex(x => x.Numero)
            .IsUnique()
            .HasDatabaseName("UX_ConteosInventario_Numero");
        builder.HasIndex(x => new { x.AlmacenId, x.Estado })
            .HasDatabaseName("IX_ConteosInventario_Almacen_Estado");
        builder.HasIndex(x => new { x.Tipo, x.Estado })
            .HasDatabaseName("IX_ConteosInventario_Tipo_Estado");
        builder.HasIndex(x => x.UbicacionAlmacenId)
            .HasDatabaseName("IX_ConteosInventario_UbicacionAlmacenId");
        builder.HasIndex(x => x.CategoriaId)
            .HasDatabaseName("IX_ConteosInventario_CategoriaId");

        builder.HasOne(x => x.Almacen)
            .WithMany()
            .HasForeignKey(x => x.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConteosInventario_Almacenes_AlmacenId");

        builder.HasOne(x => x.UbicacionAlmacen)
            .WithMany()
            .HasForeignKey(x => new { x.AlmacenId, x.UbicacionAlmacenId })
            .HasPrincipalKey(x => new { x.AlmacenId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConteosInventario_Ubicacion_MismoAlmacen");

        builder.HasOne(x => x.Categoria)
            .WithMany()
            .HasForeignKey(x => x.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_ConteosInventario_Categorias_CategoriaId");

        builder.HasMany(x => x.Detalles)
            .WithOne(x => x.ConteoInventario)
            .HasForeignKey(x => new { x.ConteoInventarioId, x.AlmacenId })
            .HasPrincipalKey(x => new { x.Id, x.AlmacenId })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ConteoInventarioDetalles_Conteo_MismoAlmacen");
    }
}
