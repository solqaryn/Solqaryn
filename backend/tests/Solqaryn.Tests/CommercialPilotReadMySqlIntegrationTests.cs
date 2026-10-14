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
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Complementa la prueba A/B de EF y el middleware InMemory:
/// combina petición HTTP de piloto, RBAC simulado y SQL físico MySQL efímero.
/// No certifica otros endpoints privados ni enciende flags de Render.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CommercialPilotReadMySqlIntegrationTests
{
    [Fact]
    public async Task Piloto_HTTP_A_B_Lee_Solo_Su_Producto_En_MySql_Y_Niega_Spoofing()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero requerido para la aceptación A/B.");
        var connection = new MySqlConnectionStringBuilder(root)
        {
            Database = "p4_pilot_http_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, mysql =>
                mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int empresaAId, empresaBId, productoAId, productoBId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("Piloto real A");
                var b = new Empresa("Piloto real B");
                setup.Set<Empresa>().AddRange(a, b);
                await setup.SaveChangesAsync();
                empresaAId = a.Id;
                empresaBId = b.Id;
            }

            async Task<int> CrearProductoAsync(int empresaId, string nombre)
            {
                var contexto = new DefaultHttpContext();
                contexto.Items[Solqaryn.Application.Security.VerifiedCommercialTenant
                    .AuthorizedEmpresaIdItemKey] = empresaId;
                var accessor = new HttpContextAccessor { HttpContext = contexto };
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: accessor);
                var categoria = new Categoria { Nombre = "Categoría " + nombre };
                db.Categorias.Add(categoria);
                await db.SaveChangesAsync();
                var producto = new Producto
                {
                    Nombre = nombre, Precio = 90m, Costo = 40m,
                    CategoriaId = categoria.Id
                };
                db.Productos.Add(producto);
                await db.SaveChangesAsync();
                Assert.Equal(empresaId, producto.EmpresaId);
                return producto.Id;
            }

            productoAId = await CrearProductoAsync(empresaAId, "Confidencial A");
            productoBId = await CrearProductoAsync(empresaBId, "Confidencial B");

            async Task<(int Status, int? Marca, int[] Visibles)> SolicitarAsync(
                string metodo, string ruta, int tenantHeader, string usuario,
                bool habilitado = true)
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["MultiEmpresa:PrivateReadPilotEnabled"] = habilitado.ToString()
                    })
                    .Build();
                var ctx = new DefaultHttpContext();
                ctx.Request.Method = metodo;
                ctx.Request.Path = ruta;
                ctx.Request.Headers["X-Empresa-Id"] = tenantHeader.ToString();
                ctx.Response.Body = new MemoryStream();
                ctx.User = usuario.Length > 0
                    ? new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario)
                    }, "TestJWT"))
                    : new ClaimsPrincipal(new ClaimsIdentity());

                var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
                permisos.Setup(x => x.TienePermisoAsync(
                        It.IsAny<int>(), ModuloSistema.Productos, AccionPermiso.Ver))
                    .Returns((int solicitado, ModuloSistema _, AccionPermiso __) =>
                        Task.FromResult((usuario == "user-a" && solicitado == empresaAId) ||
                                        (usuario == "user-b" && solicitado == empresaBId)));

                using var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(config)
                    .AddSingleton<IPermisoService>(permisos.Object)
                    .BuildServiceProvider();
                ctx.RequestServices = services;
                var accessor = new HttpContextAccessor { HttpContext = ctx };
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: accessor);
                int[] visible = Array.Empty<int>();
                var called = false;
                var middleware = new UnscopedCommercialTenantGuardMiddleware(async request =>
                {
                    called = true;
                    visible = await db.Productos.AsNoTracking()
                        .OrderBy(p => p.Id).Select(p => p.Id).ToArrayAsync();
                    request.Response.StatusCode = StatusCodes.Status200OK;
                });

                await middleware.InvokeAsync(ctx, db);
                if (ctx.Response.StatusCode == 503)
                    Assert.False(called);
                return (ctx.Response.StatusCode,
                    Solqaryn.Application.Security.VerifiedCommercialTenant.Resolve(ctx),
                    visible);
            }

            var aRead = await SolicitarAsync("GET", "/productos", empresaAId, "user-a");
            Assert.Equal(200, aRead.Status);
            Assert.Equal(empresaAId, aRead.Marca);
            Assert.Equal(new[] { productoAId }, aRead.Visibles);

            var bRead = await SolicitarAsync("GET", "/productos", empresaBId, "user-b");
            Assert.Equal(200, bRead.Status);
            Assert.Equal(empresaBId, bRead.Marca);
            Assert.Equal(new[] { productoBId }, bRead.Visibles);

            var spoof = await SolicitarAsync("GET", "/productos", empresaBId, "user-a");
            Assert.Equal(503, spoof.Status);
            Assert.Null(spoof.Marca);
            Assert.Empty(spoof.Visibles);

            foreach (var denied in new[]
            {
                await SolicitarAsync("GET", "/productos", empresaAId, "user-a", false),
                await SolicitarAsync("POST", "/productos", empresaAId, "user-a"),
                await SolicitarAsync("GET", "/productos/1", empresaAId, "user-a"),
                await SolicitarAsync("GET", "/ventas", empresaAId, "user-a"),
                await SolicitarAsync("GET", "/productos", empresaAId, "")
            })
            {
                Assert.Equal(503, denied.Status);
                Assert.Null(denied.Marca);
                Assert.Empty(denied.Visibles);
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
