using System.Reflection;
using Solqaryn.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Solqaryn.Tests;

public class N17ConteoInventarioMigrationContractTests
{
    [Fact]
    public void Migracion_N17_Tiene_Id_Canonico_Y_Esta_Registrada_Como_Migration()
    {
        var tipo = typeof(N1_7_ConteoInventarioPersistencia);
        var atributo = tipo.GetCustomAttribute<MigrationAttribute>();

        Assert.NotNull(atributo);
        Assert.Equal("20260816164800_N1_7_ConteoInventarioPersistencia", atributo!.Id);
        Assert.True(typeof(Migration).IsAssignableFrom(tipo));
    }

    [Fact]
    public void Snapshot_Canonico_Incluye_Parte_N17()
    {
        var ensamblado = typeof(N1_7_ConteoInventarioPersistencia).Assembly;
        var metodo = ensamblado
            .GetType("Solqaryn.Infrastructure.Migrations.AppDbContextSnapshotN14D")?
            .GetMethod("ApplyPart9", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(metodo);
    }
}
