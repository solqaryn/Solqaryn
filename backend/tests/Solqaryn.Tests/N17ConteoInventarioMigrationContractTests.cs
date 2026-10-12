using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public class N17ConteoInventarioMigrationContractTests
{
    [Fact]
    public void Migracion_N17_Tiene_Id_Canonico_Y_SeConservaComoHistoria()
    {
        var tipo = typeof(N1_7_ConteoInventarioPersistencia);
        var atributo = tipo.GetCustomAttribute<MigrationAttribute>();

        Assert.NotNull(atributo);
        Assert.Equal("20260816164800_N1_7_ConteoInventarioPersistencia", atributo!.Id);
        Assert.True(typeof(Migration).IsAssignableFrom(tipo));
    }

    [Fact]
    public void ModeloOracleVigente_ConservaContratoConteoInventario()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=phase7_model_only;User=root;SslMode=Disabled;")
            .Options;

        using var context = new AppDbContext(options);
        var model = context.GetService<IDesignTimeModel>().Model;

        Assert.NotNull(model.FindEntityType(typeof(ConteoInventario)));
        Assert.NotNull(model.FindEntityType(typeof(ConteoInventarioDetalle)));
    }
}
