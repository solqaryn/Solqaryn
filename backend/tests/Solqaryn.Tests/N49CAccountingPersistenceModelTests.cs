using MySql.EntityFrameworkCore.Extensions;
using System.Linq;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N49CAccountingPersistenceModelTests
{
    private readonly IModel _model;

    public N49CAccountingPersistenceModelTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=dummy;", mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        var context = new AppDbContext(options);
        _model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void ConfiguracionContable_DebeTenerMapeoCorrecto()
    {
        var entityType = _model.FindEntityType(typeof(ConfiguracionContable));
        Assert.NotNull(entityType);

        Assert.Equal("ConfiguracionesContables", entityType.GetTableName());

        var index = entityType.GetIndexes().SingleOrDefault(i => i.Properties.Any(p => p.Name == "Evento"));
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
        Assert.Equal("UX_ConfiguracionesContables_Evento", index.GetDatabaseName());

        var activoProp = entityType.FindProperty("Activo");
        Assert.NotNull(activoProp);
        Assert.Equal(true, activoProp.GetDefaultValue());

        var debeFk = entityType.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == "CuentaDebeId"));
        Assert.NotNull(debeFk);
        Assert.Equal(DeleteBehavior.Restrict, debeFk.DeleteBehavior);

        var haberFk = entityType.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(p => p.Name == "CuentaHaberId"));
        Assert.NotNull(haberFk);
        Assert.Equal(DeleteBehavior.Restrict, haberFk.DeleteBehavior);
    }

    [Fact]
    public void PeriodoContable_DebeTenerMapeoCorrecto()
    {
        var entityType = _model.FindEntityType(typeof(PeriodoContable));
        Assert.NotNull(entityType);

        Assert.Equal("PeriodosContables", entityType.GetTableName());

        var uniqueIndex = entityType.GetIndexes().SingleOrDefault(i => i.IsUnique && i.Properties.Count == 2 && i.Properties.Any(p => p.Name == "FechaInicio") && i.Properties.Any(p => p.Name == "FechaFin"));
        Assert.NotNull(uniqueIndex);
        Assert.Equal("UX_PeriodosContables_Rango", uniqueIndex.GetDatabaseName());

        var checks = entityType.GetCheckConstraints();
        Assert.Contains(checks, c => c.Name == "CK_PeriodosContables_Rango" && c.Sql == "`FechaFin` >= `FechaInicio`");
        Assert.Contains(checks, c => c.Name == "CK_PeriodosContables_Estado" && c.Sql == "`Estado` IN (1, 2)");
        Assert.Contains(checks, c => c.Name == "CK_PeriodosContables_Cierre" && c.Sql == "(`Estado` = 1 AND `CerradoEnUtc` IS NULL) OR (`Estado` = 2 AND `CerradoEnUtc` IS NOT NULL)");
    }
}
