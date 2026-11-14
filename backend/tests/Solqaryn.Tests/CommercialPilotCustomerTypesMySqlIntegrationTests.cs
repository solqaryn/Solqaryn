using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Piloto A/B para tipos de cliente y clientes, con datos MySQL descartables.
/// La autenticación/RBAC de este test es simulada explícitamente: no sustituye
/// aceptación de login real ni habilita el resto del comercio.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CommercialPilotCustomerTypesMySqlIntegrationTests
{
    [Fact]
    public async Task TipoClienteYCliente_SePersistenConOwnerCorrecto_Y_BNoSeVeDesdeA()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero es obligatorio.");
        var origin = new MySqlConnectionStringBuilder(root);
        if (!string.Equals(origin.Server, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(origin.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test destructivo sólo localhost; nunca Aiven.");
        var connection = new MySqlConnectionStringBuilder(root)
        {
            Database = "p4_types_ab_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, mysql =>
                mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            int aId, bId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("Tipos empresa A");
                var b = new Empresa("Tipos empresa B");
                setup.Set<Empresa>().AddRange(a, b);
                await setup.SaveChangesAsync();
                aId = a.Id;
                bId = b.Id;
            }

            async Task<(int Status, int? Owner, int? NuevoId, int[] Visibles)> RequestAsync(
                string usuario, int empresaId, string metodo, string ruta,
                int? tipoClienteId = null, bool lecturaHabilitada = true,
                bool escrituraHabilitada = true)
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["MultiEmpresa:PrivateReadPilotEnabled"] = lecturaHabilitada.ToString(),
                        ["MultiEmpresa:PrivateWritePilotEnabled"] = escrituraHabilitada.ToString()
                    }).Build();
                var http = new DefaultHttpContext();
                http.Request.Method = metodo;
                http.Request.Path = ruta;
                http.Request.Headers["X-Empresa-Id"] = empresaId.ToString();
                http.Response.Body = new MemoryStream();
                http.User = usuario.Length == 0
                    ? new ClaimsPrincipal(new ClaimsIdentity())
                    : new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario)
                    }, "TestJWT"));

                var perms = new Mock<IPermisoService>(MockBehavior.Strict);
                perms.Setup(x => x.TienePermisoAsync(
                    It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                    .Returns((int requested, ModuloSistema module, AccionPermiso action) =>
                        Task.FromResult(
                            (usuario == "user-a" && requested == aId ||
                             usuario == "user-b" && requested == bId) &&
                            (module == ModuloSistema.TiposClientes ||
                             module == ModuloSistema.Clientes) &&
                            (action == AccionPermiso.Ver || action == AccionPermiso.Crear)));
                using var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(config)
                    .AddSingleton<IPermisoService>(perms.Object)
                    .BuildServiceProvider();
                http.RequestServices = services;
                var accessor = new HttpContextAccessor { HttpContext = http };
                await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);
                int? created = null;
                int[] visible = Array.Empty<int>();
                var invoked = false;
                var middleware = new UnscopedCommercialTenantGuardMiddleware(async req =>
                {
                    invoked = true;
                    Assert.Equal(empresaId, VerifiedCommercialTenant.Resolve(req));
                    if (metodo == "POST" && ruta == "/tipo-clientes")
                    {
                        var tipo = new TipoCliente
                        {
                            Codigo = "GENERAL", Nombre = "General",
                            NombreNormalizado = "GENERAL", ColorHex = "#FFFFFF",
                            Activo = true, EsPredeterminado = true
                        };
                        db.TipoClientes.Add(tipo);
                        await db.SaveChangesAsync();
                        created = tipo.Id;
                    }
                    else if (metodo == "POST" && ruta == "/clientes")
                    {
                        var cliente = new Cliente
                        {
                            Nombre = "Cliente prueba",
                            TipoClienteId = tipoClienteId!.Value,
                            EmpresaId = VerifiedCommercialTenant.Resolve(req)
                        };
                        db.Clientes.Add(cliente);
                        await db.SaveChangesAsync();
                        created = cliente.Id;
                    }
                    else if (metodo == "GET" && ruta.StartsWith("/tipo-clientes", StringComparison.Ordinal))
                    {
                        IQueryable<TipoCliente> query = db.TipoClientes.AsNoTracking();
                        var parts = ruta.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2 && int.TryParse(parts[1], out var wanted))
                            query = query.Where(t => t.Id == wanted);
                        visible = await query.OrderBy(t => t.Id).Select(t => t.Id).ToArrayAsync();
                    }
                    req.Response.StatusCode = metodo == "POST" ? 201 : 200;
                });
                await middleware.InvokeAsync(http, db);
                if (http.Response.StatusCode == 503)
                    Assert.False(invoked);
                return (http.Response.StatusCode,
                    VerifiedCommercialTenant.Resolve(http), created, visible);
            }

            var typeA = await RequestAsync("user-a", aId, "POST", "/tipo-clientes");
            var typeB = await RequestAsync("user-b", bId, "POST", "/tipo-clientes");
            Assert.Equal(201, typeA.Status);
            Assert.Equal(201, typeB.Status);
            Assert.True(typeA.NuevoId > 0);
            Assert.True(typeB.NuevoId > 0);

            var clientA = await RequestAsync(
                "user-a", aId, "POST", "/clientes", typeA.NuevoId);
            var clientB = await RequestAsync(
                "user-b", bId, "POST", "/clientes", typeB.NuevoId);
            Assert.Equal(201, clientA.Status);
            Assert.Equal(201, clientB.Status);

            var readA = await RequestAsync("user-a", aId, "GET", "/tipo-clientes");
            var readB = await RequestAsync("user-b", bId, "GET", "/tipo-clientes/activos");
            Assert.Equal(new[] { typeA.NuevoId!.Value }, readA.Visibles);
            Assert.Equal(new[] { typeB.NuevoId!.Value }, readB.Visibles);
            var detailForeign = await RequestAsync(
                "user-a", aId, "GET", $"/tipo-clientes/{typeB.NuevoId}");
            Assert.Equal(200, detailForeign.Status);
            Assert.Empty(detailForeign.Visibles);

            var spoof = await RequestAsync(
                "user-a", bId, "POST", "/tipo-clientes");
            Assert.Equal(503, spoof.Status);
            Assert.Null(spoof.Owner);
            Assert.Null(spoof.NuevoId);

            foreach (var blocked in new[]
            {
                await RequestAsync("user-a", aId, "POST", "/tipo-clientes", escrituraHabilitada: false),
                await RequestAsync("user-a", aId, "GET", "/tipo-clientes", lecturaHabilitada: false),
                await RequestAsync("", aId, "GET", "/tipo-clientes"),
                await RequestAsync("user-a", aId, "POST", "/tipo-clientes/1"),
                await RequestAsync("user-a", aId, "GET", "/tipo-clientes/1/exportar")
            })
            {
                Assert.Equal(503, blocked.Status);
                Assert.Null(blocked.Owner);
            }

            await using (var verify = new AppDbContext(options))
            {
                // La inicialización MySQL conserva tipos del sistema. Este piloto
                // certifica SOLO los dos tipos empresariales creados aquí.
                var createdIds = new[] { typeA.NuevoId!.Value, typeB.NuevoId!.Value };
                var types = await verify.TipoClientes.IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(t => createdIds.Contains(t.Id))
                    .OrderBy(t => t.Id)
                    .Select(t => new { t.Id, Owner = EF.Property<int?>(t, "EmpresaId") })
                    .ToArrayAsync();
                Assert.Equal(createdIds.OrderBy(x => x), types.Select(t => t.Id));
                Assert.Equal(2, types.Length);
                Assert.Equal(aId, types.Single(t => t.Id == typeA.NuevoId.Value).Owner);
                Assert.Equal(bId, types.Single(t => t.Id == typeB.NuevoId.Value).Owner);

                var clients = await verify.Clientes.IgnoreQueryFilters()
                    .AsNoTracking().OrderBy(c => c.Id).ToArrayAsync();
                Assert.Equal(2, clients.Length);
                Assert.Equal(aId, clients[0].EmpresaId);
                Assert.Equal(bId, clients[1].EmpresaId);
                Assert.Equal(typeA.NuevoId, clients[0].TipoClienteId);
                Assert.Equal(typeB.NuevoId, clients[1].TipoClienteId);
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
