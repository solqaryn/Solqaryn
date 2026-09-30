using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N21SolicitudCompraPersistenceModelTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n21-solicitud-compra-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Cabecera_tiene_tabla_numero_unico_y_proveedor_restrictivo()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(SolicitudCompra));

        Assert.NotNull(entity);
        Assert.Equal("SolicitudesCompra", entity!.GetTableName());

        var numero = entity.FindProperty(nameof(SolicitudCompra.NumeroSolicitud));
        Assert.NotNull(numero);
        Assert.Equal(40, numero!.GetMaxLength());
        Assert.False(numero.IsNullable);

        var indiceNumero = entity.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(SolicitudCompra.NumeroSolicitud) }));
        Assert.True(indiceNumero.IsUnique);

        var fkProveedor = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(SolicitudCompra.ProveedorId) }));
        Assert.Equal(DeleteBehavior.Restrict, fkProveedor.DeleteBehavior);
    }

    [Fact]
    public void Detalle_tiene_precision_documental_y_relaciones_restrictivas()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(SolicitudCompraDetalle));

        Assert.NotNull(entity);
        Assert.Equal("SolicitudCompraDetalles", entity!.GetTableName());

        var cantidad = entity.FindProperty(nameof(SolicitudCompraDetalle.CantidadSolicitada));
        var costo = entity.FindProperty(nameof(SolicitudCompraDetalle.CostoEstimadoUnitario));
        Assert.Equal(18, cantidad!.GetPrecision());
        Assert.Equal(4, cantidad.GetScale());
        Assert.Equal(18, costo!.GetPrecision());
        Assert.Equal(4, costo.GetScale());

        var fkProducto = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(SolicitudCompraDetalle.ProductoId) }));
        var fkVariante = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(SolicitudCompraDetalle.ProductoVarianteId) }));
        var fkCabecera = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(SolicitudCompraDetalle.SolicitudCompraId) }));

        Assert.Equal(DeleteBehavior.Restrict, fkProducto.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkVariante.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, fkCabecera.DeleteBehavior);
    }

    [Fact]
    public void Modelo_no_crea_relaciones_de_inventario_kardex_costeo_o_finanzas()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(SolicitudCompra));
        var nombresNavegacion = entity!.GetNavigations().Select(n => n.Name).ToHashSet();

        Assert.DoesNotContain("MovimientosInventario", nombresNavegacion);
        Assert.DoesNotContain("MovimientosFinancieros", nombresNavegacion);
        Assert.DoesNotContain("CapasCosto", nombresNavegacion);
        Assert.DoesNotContain("Compra", nombresNavegacion);
    }
}
