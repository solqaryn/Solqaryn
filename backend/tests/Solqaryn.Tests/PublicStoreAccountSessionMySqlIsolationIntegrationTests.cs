using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Una sesión pública de la tienda A no autentica en B ni al revés.
/// Las empresas, cuentas y tokens existen solo en MySQL efímero de CI.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublicStoreAccountSessionMySqlIsolationIntegrationTests
{
    [Fact]
    public async Task TokenDeCuentaA_NuncaAutenticaEnTiendaB_NiSeCruzanPerfiles()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Se requiere MySQL efímero de CI.");
        var cs = new MySqlConnectionStringBuilder(root)
        {
            Database = "p4_store_session_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int aId, bId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("Tienda sesión A");
                var b = new Empresa("Tienda sesión B");
                setup.Set<Empresa>().AddRange(a, b);
                await setup.SaveChangesAsync();
                aId = a.Id;
                bId = b.Id;
            }

            const string tokenA = "session-test-a-token-abcdefghijklmnopqrstuvwxyz-123456789";
            const string tokenB = "session-test-b-token-abcdefghijklmnopqrstuvwxyz-123456789";

            async Task CrearCuentaSesionAsync(int empresa, string correo, string token)
            {
                var http = new DefaultHttpContext();
                http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresa;
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
                var tipo = new TipoCliente
                {
                    Codigo = "CUENTA_P4_" + empresa,
                    Nombre = "Cuenta P4 " + empresa,
                    NombreNormalizado = "CUENTA_P4_" + empresa
                };
                db.TipoClientes.Add(tipo);
                await db.SaveChangesAsync();
                var cliente = new Cliente { Nombre = "Cuenta P4 " + empresa, TipoClienteId = tipo.Id };
                db.Clientes.Add(cliente);
                await db.SaveChangesAsync();
                var cuenta = new TiendaCuentaCliente
                {
                    ClienteId = cliente.Id,
                    Nombre = "Cuenta P4 " + empresa,
                    Correo = correo,
                    CorreoNormalizado = correo.ToLowerInvariant(),
                    PasswordHash = "hash-sintetico-sin-uso",
                    Activa = true
                };
                db.TiendaCuentasCliente.Add(cuenta);
                await db.SaveChangesAsync();
                db.TiendaSesionesCliente.Add(new TiendaSesionCliente
                {
                    CuentaClienteId = cuenta.Id,
                    TokenHash = Convert.ToHexString(
                        SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant(),
                    ExpiraUtc = DateTime.UtcNow.AddHours(1)
                });
                await db.SaveChangesAsync();
            }

            await CrearCuentaSesionAsync(aId, "a@example.test", tokenA);
            await CrearCuentaSesionAsync(bId, "b@example.test", tokenB);

            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PublicStore:EnableMultiEmpresaBinding"] = "true",
                    ["PublicStore:HostBindings:0:Host"] = "a.example.test",
                    ["PublicStore:HostBindings:0:EmpresaId"] = aId.ToString(),
                    ["PublicStore:HostBindings:1:Host"] = "b.example.test",
                    ["PublicStore:HostBindings:1:EmpresaId"] = bId.ToString()
                }).Build();
            var resolver = new ConfiguredPublicStoreHostTenantResolver(config);
            var accessor = new HttpContextAccessor();
            await using var scopedDb = new AppDbContext(options,
                tenantHttpContextAccessor: accessor);
            var status = -1;
            var middleware = new UnscopedCommercialTenantGuardMiddleware(async ctx =>
            {
                var controller = new TiendaCuentaController(
                    scopedDb, Mock.Of<ITipoClientePredeterminadoResolver>())
                {
                    ControllerContext = new ControllerContext { HttpContext = ctx }
                };
                var action = await controller.Perfil();
                status = action switch
                {
                    OkObjectResult => 200,
                    UnauthorizedObjectResult => 401,
                    _ => throw new InvalidOperationException("Unexpected profile response.")
                };
            });

            async Task<int> PerfilDesdeHostAsync(string host, string token)
            {
                status = -1;
                var http = new DefaultHttpContext();
                http.Request.Method = "GET";
                http.Request.Path = "/tienda/cuenta";
                http.Request.Host = new HostString(host);
                http.Request.Headers["X-Storefront-Session"] = token;
                http.Request.Headers["X-Empresa-Id"] = bId.ToString();
                http.Request.Headers["X-Forwarded-Host"] = "b.example.test";
                http.Response.Body = new MemoryStream();
                accessor.HttpContext = http;
                await middleware.InvokeAsync(http, scopedDb, resolver);
                return status >= 0 ? status : http.Response.StatusCode;
            }

            Assert.Equal(200, await PerfilDesdeHostAsync("a.example.test", tokenA));
            Assert.Equal(401, await PerfilDesdeHostAsync("b.example.test", tokenA));
            Assert.Equal(200, await PerfilDesdeHostAsync("b.example.test", tokenB));
            Assert.Equal(401, await PerfilDesdeHostAsync("a.example.test", tokenB));
            Assert.Equal(503, await PerfilDesdeHostAsync("host-desconocido.example.test", tokenA));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
