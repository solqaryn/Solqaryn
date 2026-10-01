using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public sealed class TiendaCuentaClienteConfiguration : IEntityTypeConfiguration<TiendaCuentaCliente>
{
    public void Configure(EntityTypeBuilder<TiendaCuentaCliente> builder)
    {
        builder.ToTable("TiendaCuentasCliente");
        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Correo).IsRequired().HasMaxLength(160);
        builder.Property(x => x.CorreoNormalizado).IsRequired().HasMaxLength(160);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.CorreoNormalizado).IsUnique().HasDatabaseName("UX_TiendaCuentasCliente_Correo");
        builder.HasIndex(x => x.ClienteId).IsUnique().HasDatabaseName("UX_TiendaCuentasCliente_Cliente");
        builder.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TiendaSesionClienteConfiguration : IEntityTypeConfiguration<TiendaSesionCliente>
{
    public void Configure(EntityTypeBuilder<TiendaSesionCliente> builder)
    {
        builder.ToTable("TiendaSesionesCliente");
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UX_TiendaSesionesCliente_TokenHash");
        builder.HasIndex(x => new { x.CuentaClienteId, x.ExpiraUtc }).HasDatabaseName("IX_TiendaSesionesCliente_Cuenta_Expira");
        builder.HasOne(x => x.CuentaCliente).WithMany(x => x.Sesiones).HasForeignKey(x => x.CuentaClienteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TiendaDireccionClienteConfiguration : IEntityTypeConfiguration<TiendaDireccionCliente>
{
    public void Configure(EntityTypeBuilder<TiendaDireccionCliente> builder)
    {
        builder.ToTable("TiendaDireccionesCliente");
        builder.Property(x => x.Alias).IsRequired().HasMaxLength(60);
        builder.Property(x => x.Recibe).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Telefono).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Direccion).IsRequired().HasMaxLength(300);
        builder.HasIndex(x => new { x.CuentaClienteId, x.Predeterminada }).HasDatabaseName("IX_TiendaDireccionesCliente_Cuenta_Predeterminada");
        builder.HasOne(x => x.CuentaCliente).WithMany(x => x.Direcciones).HasForeignKey(x => x.CuentaClienteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TiendaFavoritoClienteConfiguration : IEntityTypeConfiguration<TiendaFavoritoCliente>
{
    public void Configure(EntityTypeBuilder<TiendaFavoritoCliente> builder)
    {
        builder.ToTable("TiendaFavoritosCliente");
        builder.HasIndex(x => new { x.CuentaClienteId, x.ProductoId }).IsUnique().HasDatabaseName("UX_TiendaFavoritosCliente_Cuenta_Producto");
        builder.HasOne(x => x.CuentaCliente).WithMany(x => x.Favoritos).HasForeignKey(x => x.CuentaClienteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Cascade);
    }
}