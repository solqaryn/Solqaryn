using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class ImpuestoConfiguration : IEntityTypeConfiguration<Impuesto>
{
    public void Configure(EntityTypeBuilder<Impuesto> builder)
    {
        builder.ToTable("Impuestos");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(i => i.Codigo).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Tasa).HasPrecision(9, 4);
        builder.Property(i => i.MontoFijo).HasPrecision(18, 4);
        builder.HasIndex(i => i.Codigo).IsUnique();

        builder.HasMany(i => i.Productos).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Categorias).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Operaciones).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.ClientesExentos).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.ProveedoresExentos).WithOne().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Historial).WithOne(h => h.Impuesto).HasForeignKey(h => h.ImpuestoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImpuestoProductoConfiguration : IEntityTypeConfiguration<ImpuestoProducto>
{
    public void Configure(EntityTypeBuilder<ImpuestoProducto> builder)
    {
        builder.ToTable("ImpuestoProductos");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ImpuestoId, x.ProductoId }).IsUnique().HasDatabaseName("UX_ImpuestoProductos_Impuesto_Producto");
        builder.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImpuestoCategoriaConfiguration : IEntityTypeConfiguration<ImpuestoCategoria>
{
    public void Configure(EntityTypeBuilder<ImpuestoCategoria> builder)
    {
        builder.ToTable("ImpuestoCategorias");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ImpuestoId, x.CategoriaId }).IsUnique().HasDatabaseName("UX_ImpuestoCategorias_Impuesto_Categoria");
        builder.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImpuestoClienteConfiguration : IEntityTypeConfiguration<ImpuestoCliente>
{
    public void Configure(EntityTypeBuilder<ImpuestoCliente> builder)
    {
        builder.ToTable("ImpuestoClientes");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ImpuestoId, x.ClienteId }).IsUnique().HasDatabaseName("UX_ImpuestoClientes_Impuesto_Cliente");
        builder.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImpuestoProveedorConfiguration : IEntityTypeConfiguration<ImpuestoProveedor>
{
    public void Configure(EntityTypeBuilder<ImpuestoProveedor> builder)
    {
        builder.ToTable("ImpuestoProveedores");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ImpuestoId, x.ProveedorId }).IsUnique().HasDatabaseName("UX_ImpuestoProveedores_Impuesto_Proveedor");
        builder.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ImpuestoOperacionConfiguration : IEntityTypeConfiguration<ImpuestoOperacion>
{
    public void Configure(EntityTypeBuilder<ImpuestoOperacion> builder)
    {
        builder.ToTable("ImpuestoOperaciones");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ImpuestoId, x.Operacion }).IsUnique().HasDatabaseName("UX_ImpuestoOperaciones_Impuesto_Operacion");
    }
}

public class HistorialAplicacionImpuestoConfiguration : IEntityTypeConfiguration<HistorialAplicacionImpuesto>
{
    public void Configure(EntityTypeBuilder<HistorialAplicacionImpuesto> builder)
    {
        builder.ToTable("HistorialAplicacionImpuestos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentoTipo).IsRequired().HasMaxLength(30);
        builder.Property(x => x.BaseImponible).HasPrecision(18, 4);
        builder.Property(x => x.TasaAplicada).HasPrecision(9, 4);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.DocumentoTipo, x.DocumentoId });
    }
}

public class VentaImpuestoConfiguration : IEntityTypeConfiguration<VentaImpuesto>
{
    public void Configure(EntityTypeBuilder<VentaImpuesto> builder)
    {
        builder.ToTable("VentaImpuestos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImpuestoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ImpuestoCodigoSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.Property(x => x.BaseImponible).HasPrecision(18, 4);
        builder.Property(x => x.TasaSnapshot).HasPrecision(9, 4);
        builder.Property(x => x.IncluidoEnPrecioSnapshot).HasDefaultValue(false);
        builder.HasOne<Impuesto>().WithMany().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CompraImpuestoConfiguration : IEntityTypeConfiguration<CompraImpuesto>
{
    public void Configure(EntityTypeBuilder<CompraImpuesto> builder)
    {
        builder.ToTable("CompraImpuestos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ImpuestoNombreSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ImpuestoCodigoSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MontoAplicado).HasPrecision(18, 4);
        builder.Property(x => x.BaseImponible).HasPrecision(18, 4);
        builder.Property(x => x.TasaSnapshot).HasPrecision(9, 4);
        builder.Property(x => x.IncluidoEnPrecioSnapshot).HasDefaultValue(false);
        builder.HasOne<Impuesto>().WithMany().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.Restrict);
    }
}
