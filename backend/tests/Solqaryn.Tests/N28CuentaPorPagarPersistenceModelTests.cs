using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N28CuentaPorPagarPersistenceModelTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n28-cuentas-por-pagar-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Cuenta_por_pagar_preserva_identidad_indices_precision_y_fks()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(CuentaPorPagar));

        Assert.NotNull(entity);
        Assert.Equal("CuentasPorPagar", entity!.GetTableName());
        Assert.Equal(typeof(int), entity.FindProperty(nameof(CuentaPorPagar.Id))!.ClrType);

        var moneda = entity.FindProperty(nameof(CuentaPorPagar.Moneda));
        var monto = entity.FindProperty(nameof(CuentaPorPagar.MontoOriginal));

        Assert.NotNull(moneda);
        Assert.Equal(3, moneda!.GetMaxLength());
        Assert.False(moneda.IsNullable);
        Assert.NotNull(monto);
        Assert.Equal(18, monto!.GetPrecision());
        Assert.Equal(4, monto.GetScale());

        var facturaUnica = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(CuentaPorPagar.FacturaProveedorId) }));
        Assert.True(facturaUnica.IsUnique);
        Assert.Equal("UX_CuentasPorPagar_FacturaProveedorId", facturaUnica.GetDatabaseName());

        var fkFactura = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(CuentaPorPagar.FacturaProveedorId) }));
        var fkProveedor = entity.GetForeignKeys().Single(fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(CuentaPorPagar.ProveedorId) }));

        Assert.Equal(DeleteBehavior.Restrict, fkFactura.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, fkProveedor.DeleteBehavior);
        Assert.Null(entity.FindProperty(nameof(CuentaPorPagar.MontoAplicado)));
        Assert.Null(entity.FindProperty(nameof(CuentaPorPagar.Saldo)));
    }

    [Fact]
    public void Aplicacion_preserva_idempotencia_precision_y_cascade_solo_desde_cuenta()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(AplicacionCuentaPorPagar));

        Assert.NotNull(entity);
        Assert.Equal("AplicacionesCuentaPorPagar", entity!.GetTableName());
        Assert.Equal(typeof(int), entity.FindProperty(nameof(AplicacionCuentaPorPagar.Id))!.ClrType);
        Assert.Equal(typeof(int), entity.FindProperty(nameof(AplicacionCuentaPorPagar.CuentaPorPagarId))!.ClrType);

        var monto = entity.FindProperty(nameof(AplicacionCuentaPorPagar.Monto));
        var key = entity.FindProperty(nameof(AplicacionCuentaPorPagar.IdempotencyKey));
        Assert.NotNull(monto);
        Assert.Equal(18, monto!.GetPrecision());
        Assert.Equal(4, monto.GetScale());
        Assert.NotNull(key);
        Assert.Equal(128, key!.GetMaxLength());
        Assert.False(key.IsNullable);

        var indice = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(AplicacionCuentaPorPagar.CuentaPorPagarId),
                nameof(AplicacionCuentaPorPagar.IdempotencyKey)
            }));
        Assert.True(indice.IsUnique);
        Assert.Equal("UX_AplicacionesCuentaPorPagar_Cuenta_IdempotencyKey", indice.GetDatabaseName());

        var fk = entity.GetForeignKeys().Single();
        Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);
        Assert.Equal(typeof(CuentaPorPagar), fk.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void Modelo_no_inventa_acoplamientos_a_stock_kardex_o_finanzas()
    {
        using var context = CrearContexto();
        var cuenta = context.Model.FindEntityType(typeof(CuentaPorPagar));
        var aplicacion = context.Model.FindEntityType(typeof(AplicacionCuentaPorPagar));

        Assert.NotNull(cuenta);
        Assert.NotNull(aplicacion);

        var tiposRelacionados = cuenta!.GetForeignKeys()
            .Concat(aplicacion!.GetForeignKeys())
            .Select(fk => fk.PrincipalEntityType.ClrType.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ExistenciaVariante", tiposRelacionados);
        Assert.DoesNotContain("MovimientoInventario", tiposRelacionados);
        Assert.DoesNotContain("MovimientoFinanciero", tiposRelacionados);
        Assert.DoesNotContain("CapaCostoInventario", tiposRelacionados);
    }
}
