using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N22OrdenCompraPersistenceModelTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n22-orden-compra-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Cabecera_tiene_tabla_numero_unico_y_relaciones_restrictivas()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(OrdenCompra));

        Assert.NotNull(entity);
        Assert.Equal("OrdenesCompra", entity!.GetTableName());

        var numero = entity.FindProperty(nameof(OrdenCompra.NumeroOrden));
        Assert.NotNull(numero);
        Assert.Equal(40, numero!.GetMaxLength());
        Assert.False(numero.IsNullable);

        var moneda = entity.FindProperty(nameof(OrdenCompra.Moneda));
        Assert.NotNull(moneda);
        Assert.Equal(3, moneda!.GetMaxLength());
        Assert.False(moneda.IsNullable);

        var indiceNumero = entity.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(OrdenCompra.NumeroOrden) }));
        Assert.True(indiceNumero.IsUnique);

        var fkProveedor = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(OrdenCompra.ProveedorId) }));
        var fkSolicitud = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(OrdenCompra.SolicitudCompraId) }));

        Assert.Equal(DeleteBehavior.Restrict, fkProveedor.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkSolicitud.DeleteBehavior);
    }

    [Fact]
    public void Detalle_tiene_precisiones_documentales_y_relaciones_correctas()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(OrdenCompraDetalle));

        Assert.NotNull(entity);
        Assert.Equal("OrdenCompraDetalles", entity!.GetTableName());

        foreach (var propertyName in new[]
                 {
                     nameof(OrdenCompraDetalle.CantidadOrdenada),
                     nameof(OrdenCompraDetalle.PrecioUnitario),
                     nameof(OrdenCompraDetalle.Descuento),
                     nameof(OrdenCompraDetalle.Impuesto)
                 })
        {
            var property = entity.FindProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(18, property!.GetPrecision());
            Assert.Equal(4, property.GetScale());
        }

        var fkProducto = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(OrdenCompraDetalle.ProductoId) }));
        var fkVariante = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(OrdenCompraDetalle.ProductoVarianteId) }));
        var fkCabecera = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(OrdenCompraDetalle.OrdenCompraId) }));

        Assert.Equal(DeleteBehavior.Restrict, fkProducto.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkVariante.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, fkCabecera.DeleteBehavior);
    }

    [Fact]
    public void Modelo_permanece_documental_y_no_crea_relaciones_de_recepcion_stock_kardex_costeo_o_finanzas()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(OrdenCompra));
        Assert.NotNull(entity);

        var navegaciones = entity!.GetNavigations().Select(n => n.Name).ToHashSet();
        Assert.DoesNotContain("Recepciones", navegaciones);
        Assert.DoesNotContain("ExistenciasVariante", navegaciones);
        Assert.DoesNotContain("MovimientosInventario", navegaciones);
        Assert.DoesNotContain("MovimientosFinancieros", navegaciones);
        Assert.DoesNotContain("CapasCosto", navegaciones);
        Assert.DoesNotContain("FacturasProveedor", navegaciones);
    }
}
