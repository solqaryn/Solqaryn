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
/// Piloto de altas A/B sin tocar Aiven. Exercita middleware HTTP y dueño físico
/// MySQL efímero. La autorización se simula expresamente: no equivale a login
/// real ni habilita el resto de operaciones comerciales.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CommercialPilotWriteMySqlIntegrationTests
{
    [Fact]
    public async Task Altas_Proveedor_A_B_Respetan_RBAC_Owner_Y_Cuarentena()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero requerido para el piloto de escrituras.");
        var origin = new MySqlConnectionStringBuilder(root);
        if (!string.Equals(origin.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(origin.Server, "localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Prohibido ejecutar sobre MySQL remoto o Aiven.");

        var connection = new MySqlConnectionStringBuilder(root)
        {
            Database = "p4_pilot_write_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, mysql =>
                mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int empresaAId, empresaBId, empresaCId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("Piloto escritura A");
                var b = new Empresa("Piloto escritura B");
                var c = new Empresa("Piloto existente C");
                setup.Set<Empresa>().AddRange(a, b, c);
                await setup.SaveChangesAsync();
                empresaAId = a.Id;
                empresaBId = b.Id;
                empresaCId = c.Id;
            }

            async Task<(int Status, int? Marca, int? NuevoId)> SolicitarAsync(
                string metodo, string ruta, int header, string usuario, string nombre,
                bool escrituraHabilitada = true)
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["MultiEmpresa:PrivateReadPilotEnabled"] = "false",
                        ["MultiEmpresa:PrivateWritePilotEnabled"] = escrituraHabilitada.ToString()
                    }).Build();
                var http = new DefaultHttpContext();
                http.Request.Method = metodo;
                http.Request.Path = ruta;
                http.Request.Headers["X-Empresa-Id"] = header.ToString();
                http.Response.Body = new MemoryStream();
                http.User = usuario.Length > 0
                    ? new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario)
                    }, "TestJWT"))
                    : new ClaimsPrincipal(new ClaimsIdentity());

                var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
                permisos.Setup(p => p.TienePermisoAsync(
                        It.IsAny<int>(), It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
                    .Returns((int empresaId, ModuloSistema modulo, AccionPermiso accion) =>
                        Task.FromResult(
                            (usuario == "user-a" && empresaId == empresaAId ||
                             usuario == "user-b" && empresaId == empresaBId) &&
                            (modulo == ModuloSistema.Clientes ||
                             modulo == ModuloSistema.Proveedores) &&
                            accion == AccionPermiso.Crear));
                using var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(config)
                    .AddSingleton<IPermisoService>(permisos.Object)
                    .BuildServiceProvider();
                http.RequestServices = services;
                var accessor = new HttpContextAccessor { HttpContext = http };
                await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);
                int? creado = null;
                var llamado = false;
                var middleware = new UnscopedCommercialTenantGuardMiddleware(async req =>
                {
                    llamado = true;
                    var owner = VerifiedCommercialTenant.Resolve(req);
                    Assert.Equal(header, owner);
                    if (ruta == "/proveedores")
                    {
                        var proveedor = new Proveedor { EmpresaId = owner, Nombre = nombre };
                        db.Proveedores.Add(proveedor);
                        await db.SaveChangesAsync();
                        creado = proveedor.Id;
                    }
                    req.Response.StatusCode = StatusCodes.Status201Created;
                });
                await middleware.InvokeAsync(http, db);
                if (http.Response.StatusCode == 503)
                    Assert.False(llamado);
                return (http.Response.StatusCode, VerifiedCommercialTenant.Resolve(http), creado);
            }

            var aWrite = await SolicitarAsync(
                "POST", "/proveedores", empresaAId, "user-a", "Confidencial A");
            Assert.Equal(201, aWrite.Status);
            Assert.Equal(empresaAId, aWrite.Marca);
            Assert.True(aWrite.NuevoId > 0);

            var bWrite = await SolicitarAsync(
                "POST", "/proveedores", empresaBId, "user-b", "Confidencial B");
            Assert.Equal(201, bWrite.Status);
            Assert.Equal(empresaBId, bWrite.Marca);
            Assert.True(bWrite.NuevoId > 0);

            // La segunda ruta autorizada mantiene su propio módulo/permiso;
            // no se atribuye una escritura de Cliente a este test de Proveedor.
            var cliente = await SolicitarAsync(
                "POST", "/clientes", empresaAId, "user-a", "Cliente A");
            Assert.Equal(201, cliente.Status);
            Assert.Equal(empresaAId, cliente.Marca);

            foreach (var denied in new[]
            {
                await SolicitarAsync("POST", "/proveedores", empresaBId, "user-a", "Spoof"),
                await SolicitarAsync("POST", "/proveedores", empresaCId, "user-a", "Spoof C"),
                await SolicitarAsync("POST", "/proveedores", empresaAId, "", "SinJWT"),
                await SolicitarAsync("POST", "/proveedores", empresaAId, "user-a", "FlagOff", false),
                await SolicitarAsync("POST", "/proveedores/1", empresaAId, "user-a", "Subruta"),
                await SolicitarAsync("POST", "/productos", empresaAId, "user-a", "Producto"),
                await SolicitarAsync("POST", "/ventas", empresaAId, "user-a", "Venta"),
                await SolicitarAsync("GET", "/proveedores", empresaAId, "user-a", "Lectura")
            })
            {
                Assert.Equal(503, denied.Status);
                Assert.Null(denied.Marca);
                Assert.Null(denied.NuevoId);
            }

            await using (var verify = new AppDbContext(options))
            {
                var owners = await verify.Proveedores.IgnoreQueryFilters()
                    .AsNoTracking().OrderBy(p => p.Id)
                    .Select(p => p.EmpresaId).ToArrayAsync();
                Assert.Equal(new int?[] { empresaAId, empresaBId }, owners);
            }

            async Task<int[]> VisiblesAsync(int empresa)
            {
                var http = new DefaultHttpContext();
                http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresa;
                var accessor = new HttpContextAccessor { HttpContext = http };
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: accessor);
                return await db.Proveedores.AsNoTracking()
                    .Select(p => p.Id).ToArrayAsync();
            }
            Assert.Equal(new[] { aWrite.NuevoId!.Value }, await VisiblesAsync(empresaAId));
            Assert.Equal(new[] { bWrite.NuevoId!.Value }, await VisiblesAsync(empresaBId));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
