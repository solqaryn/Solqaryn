using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N24FacturaProveedorPersistenceModelTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n24-factura-proveedor-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Cabecera_tiene_tabla_unicidad_contextual_indices_y_fks_restrictivas()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(FacturaProveedor));

        Assert.NotNull(entity);
        Assert.Equal("FacturasProveedor", entity!.GetTableName());

        var numero = entity.FindProperty(nameof(FacturaProveedor.NumeroFactura));
        var moneda = entity.FindProperty(nameof(FacturaProveedor.Moneda));
        var proveedorSnapshot = entity.FindProperty(nameof(FacturaProveedor.ProveedorNombreSnapshot));

        Assert.NotNull(numero);
        Assert.Equal(80, numero!.GetMaxLength());
        Assert.False(numero.IsNullable);
        Assert.NotNull(moneda);
        Assert.Equal(3, moneda!.GetMaxLength());
        Assert.False(moneda.IsNullable);
        Assert.NotNull(proveedorSnapshot);
        Assert.Equal(250, proveedorSnapshot!.GetMaxLength());
        Assert.False(proveedorSnapshot.IsNullable);

        var indiceNumero = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(FacturaProveedor.ProveedorId),
                nameof(FacturaProveedor.NumeroFactura)
            }));
        Assert.True(indiceNumero.IsUnique);
        Assert.Equal("UX_FacturasProveedor_Proveedor_NumeroFactura", indiceNumero.GetDatabaseName());

        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FacturaProveedor.OrdenCompraId) }));
        Assert.Contains(entity.GetIndexes(), i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(FacturaProveedor.Estado),
                nameof(FacturaProveedor.FechaEmisionUtc)
            }));

        var fkProveedor = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FacturaProveedor.ProveedorId) }));
        var fkOrden = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FacturaProveedor.OrdenCompraId) }));

        Assert.Equal(DeleteBehavior.Restrict, fkProveedor.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkOrden.DeleteBehavior);
    }

    [Fact]
    public void Detalle_preserva_precision_documental_unicidad_y_delete_behaviors()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(FacturaProveedorDetalle));

        Assert.NotNull(entity);
        Assert.Equal("FacturaProveedorDetalles", entity!.GetTableName());

        foreach (var propertyName in new[]
                 {
                     nameof(FacturaProveedorDetalle.CantidadFacturada),
                     nameof(FacturaProveedorDetalle.PrecioUnitarioSnapshot),
                     nameof(FacturaProveedorDetalle.DescuentoSnapshot),
                     nameof(FacturaProveedorDetalle.ImpuestoSnapshot)
                 })
        {
            var property = entity.FindProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(18, property!.GetPrecision());
            Assert.Equal(4, property.GetScale());
        }

        var indiceLinea = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(FacturaProveedorDetalle.FacturaProveedorId),
                nameof(FacturaProveedorDetalle.OrdenCompraDetalleId)
            }));
        Assert.True(indiceLinea.IsUnique);
        Assert.Equal("UX_FacturaProveedorDetalles_Factura_OrdenDetalle", indiceLinea.GetDatabaseName());

        var fkCabecera = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FacturaProveedorDetalle.FacturaProveedorId) }));
        var fkOrdenDetalle = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FacturaProveedorDetalle.OrdenCompraDetalleId) }));
        var fkProducto = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FacturaProveedorDetalle.ProductoId) }));
        var fkVariante = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(FacturaProveedorDetalle.ProductoVarianteId) }));

        Assert.Equal(DeleteBehavior.Cascade, fkCabecera.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkOrdenDetalle.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkProducto.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkVariante.DeleteBehavior);
    }

    [Fact]
    public void Modelo_expone_factura_y_detalle_sin_acoplar_recepcion_stock_kardex_costeo_o_finanzas()
    {
        using var context = CrearContexto();
        var cabecera = context.Model.FindEntityType(typeof(FacturaProveedor));
        var detalle = context.Model.FindEntityType(typeof(FacturaProveedorDetalle));

        Assert.NotNull(cabecera);
        Assert.NotNull(detalle);

        var navegaciones = cabecera!.GetNavigations().Select(n => n.Name)
            .Concat(detalle!.GetNavigations().Select(n => n.Name))
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Recepciones", navegaciones);
        Assert.DoesNotContain("RecepcionesCompra", navegaciones);
        Assert.DoesNotContain("ExistenciasVariante", navegaciones);
        Assert.DoesNotContain("MovimientosInventario", navegaciones);
        Assert.DoesNotContain("MovimientosFinancieros", navegaciones);
        Assert.DoesNotContain("CapasCosto", navegaciones);
    }
}
