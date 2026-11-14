using System;
using System.Linq;
using System.Reflection;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public class N49CMigrationChainTests
{
    [Fact]
    public void MigrationAssembly_ContainsBothMigrations_InCorrectOrder()
    {
        var configContableMigrationType = typeof(N4_8_C_ConfiguracionContable);
        var periodoContableMigrationType = typeof(N4_9_PeriodoContablePersistencia);

        Assert.NotNull(configContableMigrationType);
        Assert.NotNull(periodoContableMigrationType);

        var configContableAttribute = configContableMigrationType.GetCustomAttribute<MigrationAttribute>();
        var periodoContableAttribute = periodoContableMigrationType.GetCustomAttribute<MigrationAttribute>();

        Assert.NotNull(configContableAttribute);
        Assert.NotNull(periodoContableAttribute);

        Assert.Equal("20260905061000_N4_8_C_ConfiguracionContable", configContableAttribute.Id);
        Assert.Equal("20260905093000_N4_9_PeriodoContablePersistencia", periodoContableAttribute.Id);

        Assert.True(
            string.Compare(configContableAttribute.Id, periodoContableAttribute.Id, StringComparison.Ordinal) < 0,
            "N4.8 migration should precede N4.9 migration based on their IDs.");
    }

    [Fact]
    public void N49Migration_IsAdditiveAndReversible_DoesNotRecreateN48()
    {
        var n49Migration = new N4_9_PeriodoContablePersistencia();

        var upMethod = typeof(N4_9_PeriodoContablePersistencia).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        var downMethod = typeof(N4_9_PeriodoContablePersistencia).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(upMethod);
        Assert.NotNull(downMethod);

        var mbUp = new MigrationBuilder("Microsoft.EntityFrameworkCore.MySql");
        upMethod.Invoke(n49Migration, new object[] { mbUp });

        var mbDown = new MigrationBuilder("Microsoft.EntityFrameworkCore.MySql");
        downMethod.Invoke(n49Migration, new object[] { mbDown });

        var upOperations = mbUp.Operations;
        var downOperations = mbDown.Operations;

        var createTableOp = upOperations
            .OfType<CreateTableOperation>()
            .FirstOrDefault(o => o.Name == "PeriodosContables");
        Assert.NotNull(createTableOp);

        var createConfigTableOp = upOperations
            .OfType<CreateTableOperation>()
            .FirstOrDefault(o => o.Name == "ConfiguracionesContables");
        Assert.Null(createConfigTableOp);

        var dropTableOp = downOperations
            .OfType<DropTableOperation>()
            .FirstOrDefault(o => o.Name == "PeriodosContables");
        Assert.NotNull(dropTableOp);
    }
}
