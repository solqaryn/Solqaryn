using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class PublicStoreBindingConfiguration : IEntityTypeConfiguration<PublicStoreBinding>
{
    public void Configure(EntityTypeBuilder<PublicStoreBinding> builder)
    {
        builder.ToTable("PublicStoreBindings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Host).IsRequired().HasMaxLength(253)
            .UseCollation("utf8mb4_bin");
        builder.Property(x => x.StoreKey).IsRequired().HasMaxLength(48)
            .UseCollation("utf8mb4_bin");
        builder.Property(x => x.Activa).HasDefaultValue(false);
        builder.HasIndex(x => new { x.Host, x.StoreKey })
            .IsUnique().HasDatabaseName("UX_PublicStoreBindings_Host_Key");
        builder.HasIndex(x => new { x.EmpresaId, x.Activa })
            .HasDatabaseName("IX_PublicStoreBindings_Empresa_Activa");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
