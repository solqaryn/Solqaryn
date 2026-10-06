using MySql.EntityFrameworkCore.Extensions;
using System;
using System.Linq;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class N49GPeriodoContableMigrationRegressionTests
{
    private readonly IModel _model;

    public N49GPeriodoContableMigrationRegressionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=dummy;")
            .Options;

        var context = new AppDbContext(options);
        _model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void PeriodoContable_Table_And_Properties_Are_Configured_Correctly()
    {
        var entityType = _model.FindEntityType(typeof(PeriodoContable));
        Assert.NotNull(entityType);

        Assert.Equal("PeriodosContables", entityType.GetTableName());

        var constraints = entityType.GetCheckConstraints();
        Assert.Contains(constraints, c => c.Name == "CK_PeriodosContables_Rango");
        Assert.Contains(constraints, c => c.Name == "CK_PeriodosContables_Estado");
        Assert.Contains(constraints, c => c.Name == "CK_PeriodosContables_Cierre");

        Assert.True(entityType.FindProperty("FechaInicio")?.IsColumnNullable() == false);
        Assert.True(entityType.FindProperty("FechaFin")?.IsColumnNullable() == false);
        Assert.True(entityType.FindProperty("Estado")?.IsColumnNullable() == false);

        var indexes = entityType.GetIndexes();
        Assert.Contains(indexes, i => i.GetDatabaseName() == "UX_PeriodosContables_Rango" && i.IsUnique);
        Assert.Contains(indexes, i => i.GetDatabaseName() == "IX_PeriodosContables_Estado_Rango" && !i.IsUnique);
    }

    [Fact]
    public void ConfiguracionContable_NoRegression()
    {
        var entityType = _model.FindEntityType(typeof(ConfiguracionContable));
        Assert.NotNull(entityType);

        Assert.Equal("ConfiguracionesContables", entityType.GetTableName());

        var indexes = entityType.GetIndexes();
        Assert.Contains(indexes, i => i.GetDatabaseName() == "UX_ConfiguracionesContables_Evento" && i.IsUnique);

        var fkDebe = entityType.GetForeignKeys().FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "CuentaDebeId"));
        Assert.NotNull(fkDebe);
        Assert.Equal(DeleteBehavior.Restrict, fkDebe.DeleteBehavior);

        var fkHaber = entityType.GetForeignKeys().FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "CuentaHaberId"));
        Assert.NotNull(fkHaber);
        Assert.Equal(DeleteBehavior.Restrict, fkHaber.DeleteBehavior);
    }

    [Fact]
    public void PeriodoContable_CierreIdempotency_ThrowsExceptionAndDoesNotModifyState()
    {
        var fechaInicio = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fechaFin = new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var periodo = new PeriodoContable(fechaInicio, fechaFin);

        var firstCloseDate = new DateTime(2023, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        periodo.Cerrar(firstCloseDate);

        Assert.Equal(EstadoPeriodoContable.Cerrado, periodo.Estado);
        Assert.Equal(firstCloseDate, periodo.CerradoEnUtc);

        var secondCloseDate = new DateTime(2023, 2, 2, 12, 0, 0, DateTimeKind.Utc);
        var exception = Assert.Throws<InvalidOperationException>(() => periodo.Cerrar(secondCloseDate));

        Assert.Equal("El período contable ya está cerrado.", exception.Message);

        Assert.Equal(firstCloseDate, periodo.CerradoEnUtc);
        Assert.Equal(EstadoPeriodoContable.Cerrado, periodo.Estado);
    }
}
