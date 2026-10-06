using MySql.EntityFrameworkCore.Extensions;
using System.Reflection;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N67CConfiguracionEmpresaPersistenceTests
{
    private const string MigrationId = "20260912121400_N67CConfiguracionEmpresaPersistence";

    [Fact]
    public void Migracion_EstaRegistradaYDescubriblePorEfCore()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=localhost;Database=solqaryn_n67c_discovery;User=root;Password=test;", mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        using var context = new AppDbContext(options);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();

        Assert.Contains(MigrationId, migrationsAssembly.Migrations.Keys);

        var migrationType = typeof(N67CConfiguracionEmpresaPersistence);
        var migrationAttribute = Assert.Single(
            migrationType.GetCustomAttributes(typeof(MigrationAttribute), inherit: false)
                .Cast<MigrationAttribute>());
        Assert.Equal(MigrationId, migrationAttribute.Id);

        var dbContextAttribute = Assert.Single(
            migrationType.GetCustomAttributes(typeof(DbContextAttribute), inherit: false)
                .Cast<DbContextAttribute>());
        Assert.Equal(typeof(AppDbContext), dbContextAttribute.ContextType);
    }

    [Fact]
    public void Up_MaterializaConfiguracionTenantYPlantillasSinShadowFk()
    {
        var builder = ExecuteMigrationMethod("Up");

        var empresaColumns = builder.Operations.OfType<AddColumnOperation>()
            .Where(operation => operation.Table == "Empresas")
            .ToArray();
        Assert.Contains(empresaColumns, column => column.Name == "RTN" && column.MaxLength == 50 && column.IsNullable);
        Assert.Contains(empresaColumns, column => column.Name == "Direccion" && column.MaxLength == 500 && column.IsNullable);
        Assert.Contains(empresaColumns, column => column.Name == "LogoUrl" && column.MaxLength == 1000 && column.IsNullable);
        Assert.Contains(empresaColumns, column => column.Name == "LogoPublicId" && column.MaxLength == 250 && column.IsNullable);

        var configTable = Assert.Single(builder.Operations.OfType<CreateTableOperation>(),
            operation => operation.Name == "ConfigEmpresas");
        Assert.Contains(configTable.Columns, column => column.Name == "EmpresaId" && !column.IsNullable);
        Assert.Contains(configTable.Columns, column => column.Name == "Moneda" && !column.IsNullable && column.MaxLength == 3);
        Assert.Contains(configTable.Columns, column => column.Name == "ZonaHoraria" && !column.IsNullable && column.MaxLength == 100);
        Assert.Contains(configTable.Columns, column => column.Name == "CorreoSecretoReferencia" && column.MaxLength == 500);
        Assert.Contains(configTable.Columns, column => column.Name == "Version" && !column.IsNullable);
        Assert.DoesNotContain(configTable.Columns, column => column.Name == "ConfigEmpresaId");

        var configFk = Assert.Single(configTable.ForeignKeys,
            foreignKey => foreignKey.Name == "FK_ConfigEmpresas_Empresas_EmpresaId");
        Assert.Equal("Empresas", configFk.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, configFk.OnDelete);

        var plantillaTable = Assert.Single(builder.Operations.OfType<CreateTableOperation>(),
            operation => operation.Name == "PlantillasCorreoEmpresa");
        Assert.Contains(plantillaTable.Columns, column => column.Name == "EmpresaId" && !column.IsNullable);
        Assert.Contains(plantillaTable.Columns, column => column.Name == "TipoPlantilla" && !column.IsNullable && column.MaxLength == 80);
        Assert.Contains(plantillaTable.Columns, column => column.Name == "Asunto" && !column.IsNullable && column.MaxLength == 250);
        Assert.Contains(plantillaTable.Columns, column => column.Name == "Cuerpo" && !column.IsNullable);
        Assert.DoesNotContain(plantillaTable.Columns, column => column.Name == "ConfigEmpresaId");

        var plantillaFk = Assert.Single(plantillaTable.ForeignKeys,
            foreignKey => foreignKey.Name == "FK_PlantillasCorreoEmpresa_Empresas_EmpresaId");
        Assert.Equal("Empresas", plantillaFk.PrincipalTable);
        Assert.Equal(ReferentialAction.Restrict, plantillaFk.OnDelete);

        var indexes = builder.Operations.OfType<CreateIndexOperation>().ToArray();
        Assert.Contains(indexes, index => index.Name == "UX_Empresas_RTN" && index.Table == "Empresas" && index.IsUnique);
        Assert.Contains(indexes, index => index.Name == "UX_ConfigEmpresas_EmpresaId"
            && index.Table == "ConfigEmpresas"
            && index.IsUnique
            && index.Columns.SequenceEqual(new[] { "EmpresaId" }));
        Assert.Contains(indexes, index => index.Name == "UX_PlantillasCorreoEmpresa_Empresa_Tipo"
            && index.Table == "PlantillasCorreoEmpresa"
            && index.IsUnique
            && index.Columns.SequenceEqual(new[] { "EmpresaId", "TipoPlantilla" }));
    }

    [Fact]
    public void ModeloEf_ConservaOwnershipDirectoPorEmpresaYSinRelacionSombraConfigEmpresa()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n67c-{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);
        var config = db.Model.FindEntityType(typeof(ConfigEmpresa));
        var plantilla = db.Model.FindEntityType(typeof(PlantillaCorreoEmpresa));
        Assert.NotNull(config);
        Assert.NotNull(plantilla);

        Assert.Null(config!.FindProperty("ConfigEmpresaId"));
        Assert.Null(plantilla!.FindProperty("ConfigEmpresaId"));

        var configFk = Assert.Single(config.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.Properties.Count == 1 &&
            foreignKey.Properties[0].Name == nameof(ConfigEmpresa.EmpresaId));
        Assert.Equal(DeleteBehavior.Restrict, configFk.DeleteBehavior);

        var plantillaFk = Assert.Single(plantilla.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Empresa) &&
            foreignKey.Properties.Count == 1 &&
            foreignKey.Properties[0].Name == nameof(PlantillaCorreoEmpresa.EmpresaId));
        Assert.Equal(DeleteBehavior.Restrict, plantillaFk.DeleteBehavior);
        Assert.DoesNotContain(plantilla.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ConfigEmpresa));

        var configIndex = Assert.Single(config.GetIndexes(), index =>
            index.GetDatabaseName() == "UX_ConfigEmpresas_EmpresaId");
        Assert.True(configIndex.IsUnique);

        var plantillaIndex = Assert.Single(plantilla.GetIndexes(), index =>
            index.GetDatabaseName() == "UX_PlantillasCorreoEmpresa_Empresa_Tipo");
        Assert.True(plantillaIndex.IsUnique);
        Assert.Equal(new[] { nameof(PlantillaCorreoEmpresa.EmpresaId), nameof(PlantillaCorreoEmpresa.TipoPlantilla) },
            plantillaIndex.Properties.Select(property => property.Name).ToArray());
    }

    private static MigrationBuilder ExecuteMigrationMethod(string methodName)
    {
        var migration = new N67CConfiguracionEmpresaPersistence();
        var method = typeof(N67CConfiguracionEmpresaPersistence).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        method!.Invoke(migration, new object[] { builder });
        return builder;
    }
}
