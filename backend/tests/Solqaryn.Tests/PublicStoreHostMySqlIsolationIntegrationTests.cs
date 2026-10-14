using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Prueba pública A/B real: DNS ficticio aprobado por config del runner,
/// base MySQL 8.4 descartable y mismo modelo de producción.
/// Ningún dominio, tabla o registro de DEV/QA/PROD se modifica.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublicStoreHostMySqlIsolationIntegrationTests
{
    [Fact]
    public async Task HostVerificadoDaSoloCatalogoDeSuEmpresa_yRutasPrivadasSiguenBloqueadas()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Se necesita MySQL de CI.");
        var connection = new MySqlConnectionStringBuilder(root)
        {
            Database = "p4_public_ab_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int idA, idB, categoriaA, categoriaB;
            await using (var rootDb = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(rootDb);
                var a = new Empresa("Host tienda A");
                var b = new Empresa("Host tienda B");
                rootDb.Set<Empresa>().AddRange(a, b);
                await rootDb.SaveChangesAsync();
                idA = a.Id;
                idB = b.Id;
            }

            async Task<int> CrearCategoriaAsync(int empresaId, string nombre)
            {
                var http = new DefaultHttpContext();
                http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
                var categoria = new Categoria { Nombre = nombre };
                db.Categorias.Add(categoria);
                await db.SaveChangesAsync();
                return categoria.Id;
            }

            categoriaA = await CrearCategoriaAsync(idA, "Solo catálogo A");
            categoriaB = await CrearCategoriaAsync(idB, "Solo catálogo B");

            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PublicStore:EnableMultiEmpresaBinding"] = "true",
                    ["PublicStore:HostBindings:0:Host"] = "a.shop.example.test",
                    ["PublicStore:HostBindings:0:EmpresaId"] = idA.ToString(),
                    ["PublicStore:HostBindings:1:Host"] = "b.shop.example.test",
                    ["PublicStore:HostBindings:1:EmpresaId"] = idB.ToString()
                }).Build();
            var resolver = new ConfiguredPublicStoreHostTenantResolver(config);
            var accessor = new HttpContextAccessor();
            await using var dbPublico = new AppDbContext(options,
                tenantHttpContextAccessor: accessor);
            var leidas = new List<(int TenantId, int CategoriaId)>();
            var middleware = new UnscopedCommercialTenantGuardMiddleware(async context =>
            {
                var tenant = VerifiedPublicStoreTenant.Resolve(context);
                Assert.True(tenant is > 0);
                var categorias = await dbPublico.Categorias.AsNoTracking().ToListAsync();
                var una = Assert.Single(categorias);
                leidas.Add((tenant!.Value, una.Id));
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            });

            async Task<int> SolicitarAsync(string host, string ruta)
            {
                var http = new DefaultHttpContext();
                http.Request.Method = "GET";
                http.Request.Path = ruta;
                http.Request.Host = new HostString(host);
                http.Request.Headers["X-Empresa-Id"] = idB.ToString();
                http.Request.Headers["X-Forwarded-Host"] = "b.shop.example.test";
                http.Response.Body = new MemoryStream();
                accessor.HttpContext = http;
                await middleware.InvokeAsync(http, dbPublico, resolver);
                return http.Response.StatusCode;
            }

            Assert.Equal(204, await SolicitarAsync("a.shop.example.test", "/tienda/categorias"));
            Assert.Equal(204, await SolicitarAsync("b.shop.example.test", "/tienda/categorias"));
            Assert.Equal(503, await SolicitarAsync("unbound.shop.example.test", "/tienda/categorias"));
            Assert.Equal(503, await SolicitarAsync("a.shop.example.test", "/reportes/ventas"));
            Assert.Equal(new[] { (idA, categoriaA), (idB, categoriaB) }, leidas.ToArray());
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
