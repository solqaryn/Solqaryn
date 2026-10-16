using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        // FK restrictiva para nuevas asignaciones; NULL legacy sin atribuir.
        builder.HasIndex(x => x.EmpresaId).HasDatabaseName("IX_Clientes_EmpresaId");
        // Índice compuesto de titularidad, futuro soporte para FKs empresariales.
        builder.HasIndex(x => new { x.EmpresaId, x.Id })
            .IsUnique()
            .HasDatabaseName("UX_Clientes_Empresa_Id");
        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Clientes_Empresas_EmpresaId");

        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
        builder.HasIndex(c => c.Nombre).HasDatabaseName("IX_Clientes_Nombre");
        builder.Property(c => c.Telefono).HasMaxLength(30);
        builder.Property(c => c.IdentidadORTN).HasMaxLength(50);
        builder.Property(c => c.IdentidadORTNNormalizada)
            .HasMaxLength(50)
            .HasComputedColumnSql("NULLIF(LOWER(REPLACE(REPLACE(TRIM(IdentidadORTN), '-', ''), ' ', '')), '')", stored: true);
        builder.HasIndex(c => new { c.EmpresaId, c.IdentidadORTNNormalizada })
            .IsUnique()
            .HasDatabaseName("UX_Clientes_Empresa_IdentidadORTN");
        builder.Property(c => c.Correo).HasMaxLength(150);
        builder.Property(c => c.Direccion).HasMaxLength(300);

        builder.HasMany(c => c.Ventas)
            .WithOne(v => v.Cliente)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
