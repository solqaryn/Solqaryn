using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class PoliticaCosteoInventarioConfiguration : IEntityTypeConfiguration<PoliticaCosteoInventario>
{
    public void Configure(EntityTypeBuilder<PoliticaCosteoInventario> builder)
    {
        builder.ToTable("PoliticasCosteoInventario", t =>
        {
            t.HasCheckConstraint("CK_PoliticasCosteo_Metodo", "`Metodo` IN (1,2,3)");
            t.HasCheckConstraint("CK_PoliticasCosteo_Vigencia", "`VigenteHastaUtc` IS NULL OR `VigenteHastaUtc` > `VigenteDesdeUtc`");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmpresaConfiguracionId).IsRequired();
        builder.Property(x => x.Metodo).HasConversion<int>().IsRequired();
        builder.Property(x => x.VigenteDesdeUtc).IsRequired();
        builder.Property(x => x.VigenteHastaUtc);
        builder.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.Property<int?>("EmpresaConfiguracionVigenteId")
            .HasComputedColumnSql("CASE WHEN `VigenteHastaUtc` IS NULL THEN `EmpresaConfiguracionId` ELSE NULL END", stored: true);

        builder.HasIndex("EmpresaConfiguracionVigenteId")
            .IsUnique()
            .HasDatabaseName("UX_PoliticasCosteo_Empresa_Vigente");
        builder.HasIndex(x => new { x.EmpresaConfiguracionId, x.VigenteDesdeUtc })
            .HasDatabaseName("IX_PoliticasCosteo_Empresa_Vigencia");

        builder.HasOne<EmpresaConfiguracion>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaConfiguracionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PoliticasCosteo_EmpresaConfiguracion");
    }
}
