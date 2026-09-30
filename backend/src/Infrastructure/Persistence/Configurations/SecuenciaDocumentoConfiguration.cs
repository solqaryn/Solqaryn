using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N6.6.C — persistencia tenant-aware de numeraciones documentales.
/// La identidad física del contador es Empresa + Sucursal opcional + TipoDocumento.
/// SucursalScopeKey normaliza NULL a 0 para que MySQL no permita múltiples secuencias
/// a nivel empresa por la semántica de NULL en índices UNIQUE.
/// </summary>
public sealed class SecuenciaDocumentoConfiguration : IEntityTypeConfiguration<SecuenciaDocumento>
{
    public void Configure(EntityTypeBuilder<SecuenciaDocumento> builder)
    {
        builder.ToTable("SecuenciasDocumento");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmpresaId).IsRequired();
        builder.Property(x => x.SucursalId);
        builder.Property(x => x.TipoDocumento)
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(x => x.UltimoValor)
            .IsRequired()
            .HasDefaultValue(0L)
            .IsConcurrencyToken();
        builder.Property(x => x.Prefijo)
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(x => x.LongitudNumero)
            .IsRequired()
            .HasDefaultValue(6);
        builder.Property(x => x.Activa)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<int>("SucursalScopeKey")
            .HasComputedColumnSql("IFNULL(`SucursalId`, 0)", stored: true);

        builder.HasIndex("EmpresaId", "SucursalScopeKey", "TipoDocumento")
            .IsUnique()
            .HasDatabaseName("UX_SecuenciasDocumento_Empresa_Sucursal_Tipo");

        builder.HasIndex(x => new { x.EmpresaId, x.Activa })
            .HasDatabaseName("IX_SecuenciasDocumento_Empresa_Activa");

        builder.HasIndex(x => x.SucursalId)
            .HasDatabaseName("IX_SecuenciasDocumento_SucursalId");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Sucursal>()
            .WithMany()
            .HasForeignKey(x => x.SucursalId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
