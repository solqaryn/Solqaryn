using System.Linq;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests.Infrastructure.Bancos;

public class N43CPersistenceModelTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            // Use a hardcoded server version so it doesn't try to connect
            .UseMySql("Server=localhost;Database=dummy;Uid=dummy;Pwd=dummy;", new MySqlServerVersion(new System.Version(8, 0, 21)))
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void ConciliacionBancaria_PersistenceModel_ContractTests()
    {
        using var context = GetDbContext();
        var model = context.Model;

        var entityType = model.FindEntityType(typeof(ConciliacionBancaria));
        Assert.NotNull(entityType);

        Assert.Equal("ConciliacionesBancarias", entityType.GetTableName());

        var fkCuenta = entityType.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == "CuentaBancariaId"));
        Assert.NotNull(fkCuenta);
        Assert.Equal(DeleteBehavior.Restrict, fkCuenta.DeleteBehavior);

        var saldoInicial = entityType.FindProperty("SaldoInicialBanco");
        Assert.NotNull(saldoInicial);
        Assert.Equal("decimal(18,2)", saldoInicial.GetColumnType());

        var saldoFinal = entityType.FindProperty("SaldoFinalBanco");
        Assert.NotNull(saldoFinal);
        Assert.Equal("decimal(18,2)", saldoFinal.GetColumnType());

        var observaciones = entityType.FindProperty("Observaciones");
        Assert.NotNull(observaciones);
        Assert.Equal(500, observaciones.GetMaxLength());
    }

    [Fact]
    public void MovimientoEstadoCuenta_PersistenceModel_ContractTests()
    {
        using var context = GetDbContext();
        var model = context.Model;

        var entityType = model.FindEntityType(typeof(MovimientoEstadoCuenta));
        Assert.NotNull(entityType);

        Assert.Equal("MovimientosEstadoCuenta", entityType.GetTableName());

        var fkConciliacion = entityType.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == "ConciliacionBancariaId"));
        Assert.NotNull(fkConciliacion);
        Assert.Equal(DeleteBehavior.Cascade, fkConciliacion.DeleteBehavior);

        var monto = entityType.FindProperty("Monto");
        Assert.NotNull(monto);
        Assert.Equal("decimal(18,2)", monto.GetColumnType());

        var idempotencyKey = entityType.FindProperty("IdempotencyKey");
        Assert.NotNull(idempotencyKey);
        Assert.False(idempotencyKey.IsNullable);
        Assert.Equal(100, idempotencyKey.GetMaxLength());

        var uniqueIndex = entityType.GetIndexes().SingleOrDefault(i =>
            i.IsUnique &&
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.Name == "ConciliacionBancariaId") &&
            i.Properties.Any(p => p.Name == "IdempotencyKey"));
        Assert.NotNull(uniqueIndex);

        var globalUnique = entityType.GetIndexes().Any(i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == "IdempotencyKey");
        Assert.False(globalUnique);

        var concepto = entityType.FindProperty("Concepto");
        Assert.NotNull(concepto);
        Assert.Equal(250, concepto.GetMaxLength());

        var referencia = entityType.FindProperty("Referencia");
        Assert.NotNull(referencia);
        Assert.Equal(100, referencia.GetMaxLength());

        var tipo = entityType.FindProperty("Tipo");
        Assert.NotNull(tipo);
        Assert.Equal(typeof(int), tipo.ClrType.GetEnumUnderlyingType());
    }

    [Fact]
    public void MatchConciliacion_PersistenceModel_ContractTests()
    {
        using var context = GetDbContext();
        var model = context.Model;

        var entityType = model.FindEntityType(typeof(MatchConciliacion));
        Assert.NotNull(entityType);

        Assert.Equal("MatchesConciliacion", entityType.GetTableName());

        var fkMovimientoEstadoCuenta = entityType.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == "MovimientoEstadoCuentaId"));
        Assert.NotNull(fkMovimientoEstadoCuenta);
        Assert.Equal(DeleteBehavior.Cascade, fkMovimientoEstadoCuenta.DeleteBehavior);

        var fkMovimientoFinanciero = entityType.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == "MovimientoFinancieroId"));
        Assert.NotNull(fkMovimientoFinanciero);
        Assert.Equal(DeleteBehavior.Restrict, fkMovimientoFinanciero.DeleteBehavior);

        var montoAplicado = entityType.FindProperty("MontoAplicado");
        Assert.NotNull(montoAplicado);
        Assert.Equal("decimal(18,2)", montoAplicado.GetColumnType());

        var uniqueIndex = entityType.GetIndexes().SingleOrDefault(i =>
            i.IsUnique &&
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.Name == "MovimientoEstadoCuentaId") &&
            i.Properties.Any(p => p.Name == "MovimientoFinancieroId"));
        Assert.NotNull(uniqueIndex);
    }
}
