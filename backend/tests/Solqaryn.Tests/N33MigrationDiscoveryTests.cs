using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N33MigrationDiscoveryTests
{
    private const string MigrationId = "20260824120000_N3_3_C_PedidoVentaReservaInventario";

    [Fact]
    public void MigracionN33C_EstaRegistradaYEsDescubriblePorEfCore()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=solqaryn_n33_discovery;User=root;Password=test;")
            .Options;

        using var context = new AppDbContext(options);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        Assert.Contains(MigrationId, migrationsAssembly.Migrations.Keys);

        var migrationType = typeof(N3_3_C_PedidoVentaReservaInventario);
        var migrationAttribute = Assert.Single(
            migrationType.GetCustomAttributes(typeof(MigrationAttribute), inherit: false)
                .Cast<MigrationAttribute>());
        Assert.Equal(MigrationId, migrationAttribute.Id);

        var dbContextAttribute = Assert.Single(
            migrationType.GetCustomAttributes(typeof(DbContextAttribute), inherit: false)
                .Cast<DbContextAttribute>());
        Assert.Equal(typeof(AppDbContext), dbContextAttribute.ContextType);
    }
}
