using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class ProductoVarianteTecnicaModelTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Modelo_Configura_EsTecnica_Con_Valor_Predeterminado_Falso()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductoVariante));

        var property = entityType!.FindProperty(nameof(ProductoVariante.EsTecnica));

        Assert.NotNull(property);
        Assert.Equal(false, property!.GetDefaultValue());
    }

    [Fact]
    public void Modelo_Configura_Clave_Generada_Para_Unicidad_Tecnica()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ProductoVariante));

        var property = entityType!.FindProperty("ProductoTecnicoUnico");
        var index = entityType.GetIndexes()
            .Single(x => x.GetDatabaseName() == "IX_ProductoVariantes_ProductoTecnicoUnico");

        Assert.NotNull(property);
        Assert.True(index.IsUnique);
        Assert.Equal(
            "CASE WHEN `EsTecnica` = 1 AND `Eliminado` = 0 THEN `ProductoId` ELSE NULL END",
            property!.GetComputedColumnSql());
        Assert.True(property.GetIsStored());
    }

    [Fact]
    public void Entidad_Nueva_No_Es_Tecnica_Por_Defecto()
    {
        var variante = new ProductoVariante();

        Assert.False(variante.EsTecnica);
    }

    [Fact]
    public void Migracion_Ejecuta_Preflight_Antes_Del_Primer_Ddl()
    {
        var operations = BuildUpOperations();
        var firstAddColumn = operations
            .Select((operation, index) => new { operation, index })
            .First(x => x.operation is AddColumnOperation);

        Assert.Equal(4, firstAddColumn.index);
        Assert.All(operations.Take(firstAddColumn.index), operation =>
            Assert.IsType<SqlOperation>(operation));

        var guardSql = string.Join(
            Environment.NewLine,
            operations.Take(firstAddColumn.index)
                .Cast<SqlOperation>()
                .Select(x => x.Sql));

        Assert.Contains("CREATE TEMPORARY TABLE __PreflightVarianteTecnica2C1", guardSql);
        Assert.Contains("CHECK (Violaciones = 0)", guardSql);
        Assert.Contains("INSERT INTO __PreflightVarianteTecnica2C1", guardSql);
        Assert.Contains("DROP TEMPORARY TABLE __PreflightVarianteTecnica2C1", guardSql);
    }

    [Fact]
    public void Migracion_Crea_Indice_Unico_Antes_Del_Backfill_Idempotente()
    {
        var operations = BuildUpOperations();
        var indexPosition = operations
            .Select((operation, index) => new { operation, index })
            .Single(x => x.operation is CreateIndexOperation createIndex
                && createIndex.Name == "IX_ProductoVariantes_ProductoTecnicoUnico")
            .index;

        var backfillPosition = operations
            .Select((operation, index) => new { operation, index })
            .Single(x => x.operation is SqlOperation sql
                && sql.Sql.Contains("INSERT INTO ProductoVariantes", StringComparison.Ordinal))
            .index;

        Assert.True(indexPosition < backfillPosition);

        var backfill = Assert.IsType<SqlOperation>(operations[backfillPosition]).Sql;
        Assert.Contains("CONCAT('TEC-', LPAD(p.Id, 10, '0'))", backfill);
        Assert.Contains("NOT EXISTS", backfill);
        Assert.Contains("pv.Eliminado = 0", backfill);
        Assert.Contains("'Migración Bloque 2C.1'", backfill);
    }

    private static IReadOnlyList<MigrationOperation> BuildUpOperations()
    {
        var migration = new TestableAddVarianteTecnicaProductoSimple();
        return migration.BuildUp();
    }

    private sealed class TestableAddVarianteTecnicaProductoSimple
        : AddVarianteTecnicaProductoSimple
    {
        public IReadOnlyList<MigrationOperation> BuildUp()
        {
            var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
            Up(builder);
            return builder.Operations;
        }
    }
}
