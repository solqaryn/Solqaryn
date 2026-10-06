using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Migrations.Oracle;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N33MigrationDiscoveryTests
{
    private const string HistoricalMigrationId = "20260824120000_N3_3_C_PedidoVentaReservaInventario";
    private const string OracleBaselineId = "20261006111818_OracleBaseline";

    [Fact]
    public void Runtime_DescubreSoloLaCadenaOracle_YConservaHistoriaComoAuditoria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(
                "Server=localhost;Database=solqaryn_phase7_discovery;User=root;SslMode=Disabled;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        using var context = new AppDbContext(options);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        Assert.Contains(OracleBaselineId, migrationsAssembly.Migrations.Keys);
        Assert.DoesNotContain(HistoricalMigrationId, migrationsAssembly.Migrations.Keys);

        var historicalType = typeof(N3_3_C_PedidoVentaReservaInventario);
        var historicalAttribute = Assert.Single(
            historicalType.GetCustomAttributes(typeof(MigrationAttribute), inherit: false)
                .Cast<MigrationAttribute>());
        Assert.Equal(HistoricalMigrationId, historicalAttribute.Id);

        var baselineType = typeof(OracleBaseline);
        var baselineAttribute = Assert.Single(
            baselineType.GetCustomAttributes(typeof(MigrationAttribute), inherit: false)
                .Cast<MigrationAttribute>());
        Assert.Equal(OracleBaselineId, baselineAttribute.Id);
    }
}
