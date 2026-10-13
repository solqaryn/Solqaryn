using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantNaturalKeysTests
{
    private static AppDbContext MemoryDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tenant-natural-keys-" + Guid.NewGuid().ToString("N")).Options);

    [Theory]
    [InlineData("Solqaryn.Domain.Entities.DevolucionCliente", "IdempotencyKey")]
    [InlineData("Solqaryn.Domain.Entities.DevolucionProveedor", "IdempotencyKey")]
    [InlineData("Solqaryn.Domain.Entities.DevolucionProveedor", "NumeroDevolucion")]
    [InlineData("Solqaryn.Domain.Entities.OrdenCompra", "IdempotencyKey")]
    [InlineData("Solqaryn.Domain.Entities.OrdenCompra", "NumeroOrden")]
    [InlineData("Solqaryn.Domain.Entities.RecepcionCompra", "IdempotencyKey")]
    [InlineData("Solqaryn.Domain.Entities.RecepcionCompra", "NumeroRecepcion")]
    [InlineData("Solqaryn.Domain.Entities.ReservaInventario", "Numero")]
    [InlineData("Solqaryn.Domain.Entities.SerieInventario", "NumeroSerie")]
    [InlineData("Solqaryn.Domain.Entities.SolicitudCompra", "NumeroSolicitud")]
    [InlineData("Solqaryn.Domain.Entities.Sucursal", "CodigoActivoUnico")]
    [InlineData("Solqaryn.Domain.Entities.TiendaCuentaCliente", "CorreoNormalizado")]
    [InlineData("Solqaryn.Domain.Entities.Rol", "NombreNormalizado")]
    public void Codigo_natural_se_puede_repetir_en_otra_empresa_pero_no_dentro_de_la_misma(
        string entityType, string naturalKey)
    {
        using var db = MemoryDb();
        var entity = db.Model.FindEntityType(entityType);
        Assert.NotNull(entity);
        var indices = entity!.GetIndexes().ToArray();
        Assert.Contains(indices, index => index.IsUnique &&
            index.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", naturalKey }));
        Assert.DoesNotContain(indices, index => index.IsUnique &&
            index.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { naturalKey }));
    }

    [Fact]
    public void Roles_globales_conservan_restriccion_independiente()
    {
        using var db = MemoryDb();
        var role = db.Model.FindEntityType(typeof(Rol))!;
        var global = role.FindProperty("NombreNormalizadoGlobalUnico");
        Assert.NotNull(global);
        Assert.Contains(role.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "NombreNormalizadoGlobalUnico" }));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MySql_permite_mismo_nombre_de_rol_en_empresas_distintas_y_rechaza_duplicado_local()
    {
        var name = "test_tenant_business_keys_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={name};User=root;Password=root;",
                sql => sql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        await using var db = new AppDbContext(options);
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("Empresa A");
            var b = new Empresa("Empresa B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();

            var rolA = new Rol
            {
                Nombre = "Gerente", NombreNormalizado = "GERENTE",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa
            };
            var rolB = new Rol
            {
                Nombre = "Gerente", NombreNormalizado = "GERENTE",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa
            };
            db.Roles.AddRange(rolA, rolB);
            await db.SaveChangesAsync();

            Assert.Equal(2, await db.Roles.IgnoreQueryFilters()
                .CountAsync(r => r.NombreNormalizado == "GERENTE"));

            var duplicate = new Rol
            {
                Nombre = "Gerente repetido", NombreNormalizado = "GERENTE",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa
            };
            db.Roles.Add(duplicate);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(duplicate).State = EntityState.Detached;

            // La unicidad de plantillas sin empresa no se pierde.
            var templateA = new Rol
            {
                Nombre = "Plantilla plataforma", NombreNormalizado = "SHARED_TEMPLATE",
                Ambito = AmbitoAutorizacion.Plataforma
            };
            db.Roles.Add(templateA);
            await db.SaveChangesAsync();
            var templateB = new Rol
            {
                Nombre = "Plantilla duplicada", NombreNormalizado = "SHARED_TEMPLATE",
                Ambito = AmbitoAutorizacion.Plataforma
            };
            db.Roles.Add(templateB);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
