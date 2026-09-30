using System.Reflection;
using Solqaryn.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public class N18ReservaInventarioMigrationContractTests
{
    [Fact]
    public void Migracion_Debe_Crear_Tablas_Reserva_Sin_Mutar_Existencias()
    {
        var migration = new N1_8_ReservaInventarioPersistencia();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");

        InvokeUp(migration, builder);

        var tablas = builder.Operations
            .OfType<CreateTableOperation>()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains("ReservasInventario", tablas);
        Assert.Contains("ReservaInventarioDetalles", tablas);
        Assert.Equal(2, tablas.Length);

        Assert.DoesNotContain(
            builder.Operations.OfType<SqlOperation>(),
            operation => operation.Sql.Contains("UPDATE ExistenciasVariante", StringComparison.OrdinalIgnoreCase)
                      || operation.Sql.Contains("INSERT INTO ExistenciasVariante", StringComparison.OrdinalIgnoreCase)
                      || operation.Sql.Contains("DELETE FROM ExistenciasVariante", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Migracion_Debe_Proteger_Clave_Fisica_Y_Ubicacion_Del_Mismo_Almacen()
    {
        var migration = new N1_8_ReservaInventarioPersistencia();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");

        InvokeUp(migration, builder);

        var detalle = builder.Operations
            .OfType<CreateTableOperation>()
            .Single(x => x.Name == "ReservaInventarioDetalles");

        var fkUbicacion = detalle.ForeignKeys.Single(x =>
            x.Name == "FK_ReservaDetalles_Ubicacion_MismoAlmacen");
        Assert.Equal(new[] { "AlmacenId", "UbicacionAlmacenId" }, fkUbicacion.Columns);
        Assert.Equal(new[] { "AlmacenId", "Id" }, fkUbicacion.PrincipalColumns);

        var indice = builder.Operations
            .OfType<CreateIndexOperation>()
            .Single(x => x.Name == "UX_ReservaDetalles_ClaveFisica");
        Assert.True(indice.IsUnique);
        Assert.Equal(
            new[] { "ReservaInventarioId", "ProductoVarianteId", "AlmacenId", "UbicacionNormalizada" },
            indice.Columns);
    }

    [Fact]
    public void Guardas_Temporales_Deben_Tener_Primary_Key_Para_SqlRequirePrimaryKey()
    {
        var migration = new N1_8_ReservaInventarioPersistencia();
        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");

        InvokeUp(migration, builder);

        var guardas = string.Join("\n", builder.Operations.OfType<SqlOperation>().Select(x => x.Sql));
        Assert.Contains("CREATE TEMPORARY TABLE __N18CGuard", guardas, StringComparison.Ordinal);
        Assert.Contains("CREATE TEMPORARY TABLE __N18CPostGuard", guardas, StringComparison.Ordinal);
        Assert.Contains("PRIMARY KEY", guardas, StringComparison.OrdinalIgnoreCase);
    }

    private static void InvokeUp(Migration migration, MigrationBuilder builder)
    {
        var method = migration.GetType().GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("No se encontró el método Up de la migración N1.8.C.");

        method.Invoke(migration, new object[] { builder });
    }
}
