using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class DescuentoConfiguration : IEntityTypeConfiguration<Descuento>
{
    public void Configure(EntityTypeBuilder<Descuento> builder)
    {
        builder.ToTable("Descuentos");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(d => d.CodigoPromocional).HasMaxLength(50);
        builder.Property(d => d.CodigoPromocionalNormalizado).HasMaxLength(50);
        builder.Property(d => d.Valor).HasPrecision(18, 4);
        builder.Property(d => d.MontoMinimo).HasPrecision(18, 4);
        builder.Property(d => d.MontoMaximoDescuento).HasPrecision(18, 4);

        builder.HasIndex(d => d.CodigoPromocionalNormalizado).IsUnique().HasFilter("`CodigoPromocionalNormalizado` IS NOT NULL");

        builder.HasMany(d => d.Productos).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Categorias).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Clientes).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Roles).WithOne().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Historial).WithOne(h => h.Descuento).HasForeignKey(h => h.DescuentoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DescuentoProductoConfiguration : IEntityTypeConfiguration<DescuentoProducto>
{
    public void Configure(EntityTypeBuilder<DescuentoProducto> builder)
    {
        builder.ToTable("DescuentoProductos");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DescuentoId, x.ProductoId }).IsUnique().HasDatabaseName("UX_DescuentoProductos_Descuento_Producto");
        builder.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DescuentoCategoriaConfiguration : IEntityTypeConfiguration<DescuentoCategoria>
{
    public void Configure(EntityTypeBuilder<DescuentoCategoria> builder)
    {
        builder.ToTable("DescuentoCategorias");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DescuentoId, x.CategoriaId }).IsUnique().HasDatabaseName("UX_DescuentoCategorias_Descuento_Categoria");
        builder.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DescuentoClienteConfiguration : IEntityTypeConfiguration<DescuentoCliente>
{
    public void Configure(EntityTypeBuilder<DescuentoCliente> builder)
    {
        builder.ToTable("DescuentoClientes");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DescuentoId, x.ClienteId }).IsUnique().HasDatabaseName("UX_DescuentoClientes_Descuento_Cliente");
        builder.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DescuentoRolConfiguration : IEntityTypeConfiguration<DescuentoRol>
{
    public void Configure(EntityTypeBuilder<DescuentoRol> builder)
    {
        builder.ToTable("DescuentoRoles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DescuentoId, x.RolId }).IsUnique().HasDatabaseName("UX_DescuentoRoles_Descuento_Rol");
        builder.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class HistorialUsoDescuentoConfiguration : IEntityTypeConfiguration<HistorialUsoDescuento>
{
    public void Configure(EntityTypeBuilder<HistorialUsoDescuento> builder)
    {
        builder.ToTable("HistorialUsoDescuentos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.VentaId, x.Fecha });
        builder.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VentaDescuentoConfiguration : IEntityTypeConfiguration<VentaDescuento>
{
    public void Configure(EntityTypeBuilder<VentaDescuento> builder)
    {
        builder.ToTable("VentaDescuentos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DescuentoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(x => x.DescuentoCodigoSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.Property(x => x.ValorSnapshot).HasPrecision(18, 4);
        builder.HasOne<Descuento>().WithMany().HasForeignKey(x => x.DescuentoId).OnDelete(DeleteBehavior.Restrict);
    }
}
