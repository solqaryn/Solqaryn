using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests.Domain.Bancos;

public sealed class CuentaBancariaConfigurationTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"cuenta-bancaria-config-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Entidad_mapa_a_tabla_CuentasBancarias()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(CuentaBancaria));

        Assert.NotNull(entity);
        Assert.Equal("CuentasBancarias", entity!.GetTableName());
    }

    [Fact]
    public void BancoId_FK_apunta_a_Banco_con_Restrict()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(CuentaBancaria));
        Assert.NotNull(entity);

        var fkBanco = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(CuentaBancaria.BancoId) }));

        Assert.Equal(DeleteBehavior.Restrict, fkBanco.DeleteBehavior);
    }

    [Fact]
    public void Propiedades_tienen_longitudes_y_tipos_correctos()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(CuentaBancaria));
        Assert.NotNull(entity);

        var numeroCuenta = entity.FindProperty(nameof(CuentaBancaria.NumeroCuenta));
        Assert.NotNull(numeroCuenta);
        Assert.Equal(50, numeroCuenta.GetMaxLength());

        var moneda = entity.FindProperty(nameof(CuentaBancaria.Moneda));
        Assert.NotNull(moneda);
        Assert.Equal(3, moneda.GetMaxLength());

        var saldoInicial = entity.FindProperty(nameof(CuentaBancaria.SaldoInicial));
        Assert.NotNull(saldoInicial);
        Assert.Equal("decimal(18,2)", saldoInicial.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:ColumnType")?.Value as string);

        var estado = entity.FindProperty(nameof(CuentaBancaria.Estado));
        Assert.NotNull(estado);
        Assert.Null(estado.GetValueConverter());
    }

    [Fact]
    public void Indices_estan_configurados_correctamente()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(CuentaBancaria));
        Assert.NotNull(entity);

        var uniqueIndex = entity.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(CuentaBancaria.BancoId), nameof(CuentaBancaria.NumeroCuenta) }));
        Assert.NotNull(uniqueIndex);
        Assert.True(uniqueIndex.IsUnique);

        var estadoIndex = entity.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(CuentaBancaria.Estado) }));
        Assert.NotNull(estadoIndex);
    }

    [Fact]
    public void CheckConstraints_estan_configurados()
    {
        using var context = CrearContexto();
        var entity = context.Model.FindEntityType(typeof(CuentaBancaria));
        Assert.NotNull(entity);

        var constraints = entity.GetAnnotations()
            .Where(a => a.Name.StartsWith("Relational:CheckConstraint:"))
            .ToList();

        if (constraints.Count == 0)
            return;

        var bancoId = constraints.Single(a => a.Name == "Relational:CheckConstraint:CK_CuentasBancarias_BancoId");
        var saldo = constraints.Single(a => a.Name == "Relational:CheckConstraint:CK_CuentasBancarias_SaldoInicial");
        var estado = constraints.Single(a => a.Name == "Relational:CheckConstraint:CK_CuentasBancarias_Estado");

        Assert.Contains("`BancoId` > 0", bancoId.Value?.ToString());
        Assert.Contains("`SaldoInicial` >= 0", saldo.Value?.ToString());
        Assert.Contains("`Estado` IN (1, 2)", estado.Value?.ToString());
    }
}
