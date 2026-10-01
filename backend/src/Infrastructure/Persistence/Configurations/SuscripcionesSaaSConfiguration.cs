using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

/// <summary>
/// N6.9.C — persistencia del catalogo SaaS. Plan es global al catalogo;
/// la pertenencia tenant se materializa exclusivamente en Suscripcion.EmpresaId.
/// </summary>
public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Planes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Codigo)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => x.Codigo)
            .IsUnique()
            .HasDatabaseName("UX_Planes_Codigo");

        // N6.10.C: clave principal compuesta de referencia para que una regla
        // plan -> modulo no pueda persistir un PlanId con un Codigo de otro plan.
        builder.HasAlternateKey(x => new { x.Id, x.Codigo })
            .HasName("AK_Planes_Id_Codigo");

        builder.OwnsMany(x => x.Limites, limites =>
        {
            limites.ToTable("PlanLimites");
            limites.WithOwner().HasForeignKey("PlanId");
            limites.Property<int>("PlanId");

            limites.Property(x => x.Clave)
                .IsRequired()
                .HasMaxLength(120);

            limites.Property(x => x.ValorMaximo);

            limites.HasKey("PlanId", nameof(PlanLimite.Clave));
            limites.HasIndex("PlanId")
                .HasDatabaseName("IX_PlanLimites_PlanId");
        });

        builder.Navigation(x => x.Limites)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// N6.9.C — suscripcion tenant-scoped. Todas las relaciones usan RESTRICT para
/// preservar historial y evitar cascadas entre Empresa, Plan y Suscripcion.
/// </summary>
public sealed class SuscripcionConfiguration : IEntityTypeConfiguration<Suscripcion>
{
    public void Configure(EntityTypeBuilder<Suscripcion> builder)
    {
        builder.ToTable("Suscripciones");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmpresaId).IsRequired();
        builder.Property(x => x.PlanId).IsRequired();
        builder.Property(x => x.Estado).IsRequired();
        builder.Property(x => x.InicioUtc).IsRequired();
        builder.Property(x => x.FinUtc);

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.EmpresaId, x.Estado })
            .HasDatabaseName("IX_Suscripciones_EmpresaId_Estado");

        builder.HasIndex(x => new { x.PlanId, x.Estado })
            .HasDatabaseName("IX_Suscripciones_PlanId_Estado");

        builder.HasIndex(x => new { x.EmpresaId, x.InicioUtc })
            .HasDatabaseName("IX_Suscripciones_EmpresaId_InicioUtc");

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// N6.10.C — persistencia explicita de entitlements plan -> modulo.
/// Se mantiene separada de PlanLimite y se blinda PlanId + PlanCodigo mediante
/// FK compuesta para evitar reglas divergentes que pudieran evaluarse con un
/// codigo de plan obsoleto o perteneciente a otro plan.
/// </summary>
public sealed class PlanModuloConfiguration : IEntityTypeConfiguration<PlanModulo>
{
    public void Configure(EntityTypeBuilder<PlanModulo> builder)
    {
        builder.ToTable("PlanModulos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlanId).IsRequired();

        builder.Property(x => x.PlanCodigo)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.ModuloClave)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreadoPorNombreUsuario).HasMaxLength(150);
        builder.Property(x => x.ActualizadoPorNombreUsuario).HasMaxLength(150);

        builder.HasIndex(x => new { x.PlanId, x.ModuloClave })
            .IsUnique()
            .HasDatabaseName("UX_PlanModulos_PlanId_ModuloClave");

        builder.HasIndex(x => new { x.PlanId, x.PlanCodigo })
            .HasDatabaseName("IX_PlanModulos_PlanId_PlanCodigo");

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(x => new { x.PlanId, x.PlanCodigo })
            .HasPrincipalKey(x => new { x.Id, x.Codigo })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
