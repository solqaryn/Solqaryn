using System.Reflection;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Solqaryn.Tests;

public sealed class TenantAwareDataModelPersistenceTests
{
    [Fact]
    public void Sucursal_EmpresaId_EsOpcionalDuranteTransicion_Y_FkRestrict()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n62c-{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);
        var sucursal = db.Model.FindEntityType(typeof(Sucursal));
        Assert.NotNull(sucursal);

        var empresaId = sucursal!.FindProperty(nameof(Sucursal.EmpresaId));
        Assert.NotNull(empresaId);
        Assert.True(empresaId!.IsNullable);

        var fk = Assert.Single(sucursal.GetForeignKeys(), candidate =>
            candidate.PrincipalEntityType.ClrType == typeof(Empresa) &&
            candidate.Properties.Count == 1 &&
            candidate.Properties[0].Name == nameof(Sucursal.EmpresaId));

        Assert.False(fk.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    [Fact]
    public void Ownership_Descendente_NoDuplica_EmpresaId()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n62c-{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);
        var almacen = db.Model.FindEntityType(typeof(Almacen));
        var ubicacion = db.Model.FindEntityType(typeof(UbicacionAlmacen));

        Assert.NotNull(almacen);
        Assert.NotNull(ubicacion);
        Assert.True(almacen!.FindProperty("EmpresaId")!.IsNullable);
        Assert.True(ubicacion!.FindProperty("EmpresaId")!.IsNullable);
        Assert.Contains(almacen.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Single().Name == "EmpresaId" &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(ubicacion.GetIndexes(), ix => ix.IsUnique &&
            ix.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmpresaId", "Id" }));
    }

    [Fact]
    public void Migracion_EsAtomica_Y_FallaCerrado_SinBackfillArbitrario()
    {
        var migration = new N62CTenantAwareEmpresaId();
        var up = typeof(N62CTenantAwareEmpresaId)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);

        var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql");
        up!.Invoke(migration, new object[] { builder });

        var operation = Assert.Single(builder.Operations.OfType<SqlOperation>());
        Assert.Contains("ALTER TABLE `Sucursales`", operation.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("MODIFY COLUMN", operation.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FK_Sucursales_Empresas_EmpresaId", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("REFERENCES `Empresas` (`Id`)", operation.Sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE RESTRICT", operation.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE `Sucursales`", operation.Sql, StringComparison.OrdinalIgnoreCase);
    }
}
