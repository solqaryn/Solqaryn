using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Certifica físicamente que dos tiendas pueden usar el mismo correo
/// sin compartir cuentas. Siempre en bases MySQL descartables.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublicStoreAccountEmailTenantIntegrationTests
{
    [Fact]
    public async Task MismoCorreoEnDosEmpresas_CuentasIndependientes_y_duplicadoLocalRechazado()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL de CI no configurado");
        var dbName = "p4_store_email_" + Guid.NewGuid().ToString("N");
        var connection = new MySqlConnectionStringBuilder(root) { Database = dbName }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            int aId, bId;
            await using (var rootDb = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(rootDb);
                var a = new Empresa("Tienda Alfa");
                var b = new Empresa("Tienda Beta");
                rootDb.Set<Empresa>().AddRange(a, b);
                await rootDb.SaveChangesAsync();
                aId = a.Id;
                bId = b.Id;
            }

            async Task<int> CrearCuentaAsync(int empresaId, string codigo)
            {
                var http = new DefaultHttpContext();
                http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
                var tipo = new TipoCliente
                {
                    Codigo = codigo, Nombre = codigo, NombreNormalizado = codigo
                };
                db.TipoClientes.Add(tipo);
                await db.SaveChangesAsync();

                var cliente = new Cliente { Nombre = "Cliente tienda " + codigo, TipoClienteId = tipo.Id };
                db.Clientes.Add(cliente);
                await db.SaveChangesAsync();

                var cuenta = new TiendaCuentaCliente
                {
                    ClienteId = cliente.Id,
                    Nombre = "Cliente " + codigo,
                    Correo = "persona@example.test",
                    CorreoNormalizado = "PERSONA@EXAMPLE.TEST",
                    PasswordHash = "hash-no-real-" + codigo
                };
                db.TiendaCuentasCliente.Add(cuenta);
                await db.SaveChangesAsync();
                Assert.Equal(empresaId, db.Entry(cuenta).Property<int?>("EmpresaId").CurrentValue);

                var visibles = await db.TiendaCuentasCliente.AsNoTracking()
                    .Where(x => x.CorreoNormalizado == "PERSONA@EXAMPLE.TEST")
                    .ToListAsync();
                Assert.Single(visibles);
                Assert.Equal(cuenta.Id, visibles[0].Id);
                return cuenta.Id;
            }

            var cuentaA = await CrearCuentaAsync(aId, "TENANT_A");
            var cuentaB = await CrearCuentaAsync(bId, "TENANT_B");
            Assert.NotEqual(cuentaA, cuentaB);

            var httpA = new DefaultHttpContext();
            httpA.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = aId;
            await using (var dbA = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = httpA }))
            {
                Assert.Equal(cuentaA, (await dbA.TiendaCuentasCliente.AsNoTracking().SingleAsync()).Id);

                var tipoA = await dbA.TipoClientes.AsNoTracking().FirstAsync();
                var segundoClienteA = new Cliente
                {
                    Nombre = "Otro cliente A", TipoClienteId = tipoA.Id
                };
                dbA.Clientes.Add(segundoClienteA);
                await dbA.SaveChangesAsync();

                var repetida = new TiendaCuentaCliente
                {
                    ClienteId = segundoClienteA.Id,
                    Nombre = "Duplicada A",
                    Correo = "persona@example.test",
                    CorreoNormalizado = "PERSONA@EXAMPLE.TEST",
                    PasswordHash = "prueba"
                };
                // La segunda cuenta tiene ClienteId distinto: sólo puede
                // fallar por correo duplicado dentro del mismo tenant A.
                dbA.TiendaCuentasCliente.Add(repetida);
                await Assert.ThrowsAsync<DbUpdateException>(() => dbA.SaveChangesAsync());
            }

            await using (var forensic = new AppDbContext(options))
                Assert.Equal(2, await forensic.TiendaCuentasCliente.IgnoreQueryFilters()
                    .CountAsync(x => x.CorreoNormalizado == "PERSONA@EXAMPLE.TEST"));

            // MySQL no asegura atomicidad al hacer DROP/CREATE INDEX: comprobar
            // que el rollback se niega SIN quitar primero la unicidad tenant.
            await using (var rollback = new AppDbContext(options))
            {
                var migrator = rollback.Database.GetService<IMigrator>();
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    migrator.MigrateAsync("20261013231000_CommercialTenantRemainingScopedFks"));
            }

            await using (var connectionCheck = new MySqlConnection(connection))
            {
                await connectionCheck.OpenAsync();
                await using var indexCheck = connectionCheck.CreateCommand();
                indexCheck.CommandText = """
                    SELECT COUNT(*)
                      FROM INFORMATION_SCHEMA.STATISTICS
                     WHERE TABLE_SCHEMA = DATABASE()
                       AND TABLE_NAME = 'TiendaCuentasCliente'
                       AND INDEX_NAME = 'UX_TiendaCuentasCliente_Correo'
                       AND COLUMN_NAME IN ('EmpresaId', 'CorreoNormalizado')
                    """;
                Assert.Equal(2, Convert.ToInt32(await indexCheck.ExecuteScalarAsync()));
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public void UnicidadDeCorreoDeclaradaPorEmpresaEnModelo()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-account-index-" + Guid.NewGuid().ToString("N")).Options;
        using var db = new AppDbContext(options);
        var mapped = db.Model.FindEntityType(typeof(TiendaCuentaCliente))!;
        Assert.Contains(mapped.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", "CorreoNormalizado" }));
        Assert.DoesNotContain(mapped.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(p => p.Name).SequenceEqual(new[] { "CorreoNormalizado" }));
    }
}
