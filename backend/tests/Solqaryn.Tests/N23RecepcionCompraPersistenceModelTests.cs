using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N23RecepcionCompraPersistenceModelTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n23-recepcion-compra-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Cabecera_tiene_identidades_unicas_y_fk_restrictiva_a_orden()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(RecepcionCompra));

        Assert.NotNull(entity);
        Assert.Equal("RecepcionesCompra", entity!.GetTableName());
        Assert.Equal(40, entity.FindProperty(nameof(RecepcionCompra.NumeroRecepcion))!.GetMaxLength());
        Assert.Equal(128, entity.FindProperty(nameof(RecepcionCompra.IdempotencyKey))!.GetMaxLength());
        Assert.Equal(64, entity.FindProperty(nameof(RecepcionCompra.IdempotencyFingerprint))!.GetMaxLength());

        var indiceNumero = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompra.NumeroRecepcion) }));
        var indiceIdempotencia = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompra.IdempotencyKey) }));

        Assert.True(indiceNumero.IsUnique);
        Assert.True(indiceIdempotencia.IsUnique);

        var fkOrden = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompra.OrdenCompraId) }));
        Assert.Equal(DeleteBehavior.Restrict, fkOrden.DeleteBehavior);
    }

    [Fact]
    public void Detalle_preserva_precision_balance_y_clave_fisica_null_safe()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(RecepcionCompraDetalle));

        Assert.NotNull(entity);
        Assert.Equal("RecepcionCompraDetalles", entity!.GetTableName());

        foreach (var propertyName in new[]
                 {
                     nameof(RecepcionCompraDetalle.CantidadRecibida),
                     nameof(RecepcionCompraDetalle.CantidadDanada),
                     nameof(RecepcionCompraDetalle.CantidadFaltante),
                     nameof(RecepcionCompraDetalle.CantidadSobrante),
                     nameof(RecepcionCompraDetalle.CostoUnitarioSnapshot)
                 })
        {
            var property = entity.FindProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(18, property!.GetPrecision());
            Assert.Equal(4, property.GetScale());
        }

        var ubicacionNormalizada = entity.FindProperty("UbicacionAlmacenIdUnica");
        Assert.NotNull(ubicacionNormalizada);
        Assert.Contains("IFNULL", ubicacionNormalizada!.GetComputedColumnSql()!, StringComparison.OrdinalIgnoreCase);

        var indiceFisico = entity.GetIndexes().Single(i => i.GetDatabaseName() ==
            "UX_RecepcionCompraDetalles_Recepcion_Linea_Almacen_Ubicacion");
        Assert.True(indiceFisico.IsUnique);
        Assert.Equal(
            new[] { "RecepcionCompraId", "OrdenCompraDetalleId", "AlmacenId", "UbicacionAlmacenIdUnica" },
            indiceFisico.Properties.Select(p => p.Name));
    }

    [Fact]
    public void Checks_persistentes_blindan_idempotencia_cantidades_y_costo()
    {
        using var context = CrearContexto();
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var cabecera = designTimeModel.FindEntityType(typeof(RecepcionCompra));
        var detalle = designTimeModel.FindEntityType(typeof(RecepcionCompraDetalle));

        Assert.NotNull(cabecera);
        Assert.NotNull(detalle);

        var checksCabecera = cabecera!.GetCheckConstraints().Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        var checksDetalle = detalle!.GetCheckConstraints().Select(c => c.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("CK_RecepcionesCompra_IdempotenciaAtomica", checksCabecera);
        Assert.Contains("CK_RecepcionCompraDetalles_CantidadesNoNegativas", checksDetalle);
        Assert.Contains("CK_RecepcionCompraDetalles_BalanceFisico", checksDetalle);
        Assert.Contains("CK_RecepcionCompraDetalles_ActividadFisica", checksDetalle);
        Assert.Contains("CK_RecepcionCompraDetalles_CostoNoNegativo", checksDetalle);
    }

    [Fact]
    public void Detalle_restringe_dependencias_fisicas_y_cascade_solo_desde_cabecera()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(RecepcionCompraDetalle));
        Assert.NotNull(entity);

        var fkCabecera = entity!.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompraDetalle.RecepcionCompraId) }));
        var fkOrdenDetalle = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompraDetalle.OrdenCompraDetalleId) }));
        var fkProducto = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompraDetalle.ProductoId) }));
        var fkVariante = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompraDetalle.ProductoVarianteId) }));
        var fkAlmacen = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(RecepcionCompraDetalle.AlmacenId) }));
        var fkUbicacionMismoAlmacen = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(RecepcionCompraDetalle.AlmacenId),
                nameof(RecepcionCompraDetalle.UbicacionAlmacenId)
            }));

        Assert.Equal(DeleteBehavior.Cascade, fkCabecera.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkOrdenDetalle.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkProducto.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkVariante.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkAlmacen.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkUbicacionMismoAlmacen.DeleteBehavior);
    }

    [Fact]
    public void Modelo_de_recepcion_no_materializa_stock_kardex_costeo_ni_finanzas_en_C()
    {
        using var context = CrearContexto();
        var cabecera = context.Model.FindEntityType(typeof(RecepcionCompra));
        var detalle = context.Model.FindEntityType(typeof(RecepcionCompraDetalle));

        Assert.NotNull(cabecera);
        Assert.NotNull(detalle);

        var navegaciones = cabecera!.GetNavigations().Select(n => n.Name)
            .Concat(detalle!.GetNavigations().Select(n => n.Name))
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ExistenciaVariante", navegaciones);
        Assert.DoesNotContain("MovimientosInventario", navegaciones);
        Assert.DoesNotContain("MovimientosFinancieros", navegaciones);
        Assert.DoesNotContain("CapasCosto", navegaciones);
        Assert.DoesNotContain("FacturaProveedor", navegaciones);
    }
}
