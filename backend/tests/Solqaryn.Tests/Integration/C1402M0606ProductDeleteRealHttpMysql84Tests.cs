using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C14-02.M06.06: DELETE real, JWT/RBAC A/B y reversión SQL del agregado.
/// No certifica otras superficies de aceptación ni habilita el piloto fuera del TestServer.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1402M0606ProductDeleteRealHttpMysql84Tests
{
    private const string Issuer = "c1402-m0606-ephemeral-issuer";
    private const string Audience = "c1402-m0606-ephemeral-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c1402-m0606-local-test-signing-key-isolated-2026"));

    [Fact]
    public async Task RealDeleteRequiresLiveTenantGrantAndRollsBackProductVariantsAndAudit()
    {
        var raw = Environment.GetEnvironmentVariable("C1402_M0606_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root" ||
            origin.Database != "c1402_m0606_bootstrap")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c1402_delete_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, mysql => mysql.MigrationsAssembly(
                "Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            await using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT VERSION()";
                Assert.StartsWith("8.4.", Convert.ToString(await command.ExecuteScalarAsync()));
            }
            await db.Database.CloseConnectionAsync();

            var a = new Empresa("C1402M0606 Tenant A");
            var b = new Empresa("C1402M0606 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C1402M0606 role A", NombreNormalizado = "C1402M0606_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C1402M0606 role B", NombreNormalizado = "C1402M0606_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var userA = NewUser("a", roleA.Id);
            var userB = NewUser("b", roleB.Id);
            db.Usuarios.AddRange(userA, userB);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(userA.Id, a.Id, roleA.Id),
                new UsuarioEmpresa(userB.Id, b.Id, roleB.Id));
            await db.SaveChangesAsync();

            var permission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Modulo == ModuloSistema.Productos &&
                    p.Accion == AccionPermiso.EliminarLogico &&
                    p.Ambito == AmbitoAutorizacion.Empresa && p.Activo && !p.Eliminado);
            if (permission is null)
            {
                permission = new Permiso
                {
                    Codigo = "C1402M0606.PRODUCTOS.ELIMINAR_LOGICO",
                    Nombre = "C1402 M0606 isolated permission",
                    Modulo = ModuloSistema.Productos, Accion = AccionPermiso.EliminarLogico,
                    Ambito = AmbitoAutorizacion.Empresa, Activo = true
                };
                db.Permisos.Add(permission);
                await db.SaveChangesAsync();
            }
            db.RolPermisos.AddRange(
                new RolPermiso { RolId = roleA.Id, PermisoId = permission.Id },
                new RolPermiso { RolId = roleB.Id, PermisoId = permission.Id });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            async Task<(int Product, int Variant)> SeedAsync(int tenant, string name, bool technical, int stock = 0)
            {
                var context = new DefaultHttpContext();
                context.Items[Solqaryn.Application.Security.VerifiedCommercialTenant
                    .AuthorizedEmpresaIdItemKey] = tenant;
                await using var owned = new AppDbContext(options,
                    tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = context });
                var category = new Categoria { Nombre = "C14 " + name };
                owned.Categorias.Add(category);
                await owned.SaveChangesAsync();
                var product = new Producto
                {
                    Nombre = name, CategoriaId = category.Id, Precio = 100m, Costo = 50m
                };
                owned.Productos.Add(product);
                await owned.SaveChangesAsync();
                var variant = new ProductoVariante
                {
                    EmpresaId = tenant, ProductoId = product.Id, Sku = "C14-" + name,
                    Precio = 100m, Costo = 50m, EsTecnica = technical,
                    Cantidad = stock, Activo = true
                };
                owned.ProductoVariantes.Add(variant);
                await owned.SaveChangesAsync();
                Assert.Equal(tenant, product.EmpresaId);
                Assert.Equal(tenant, variant.EmpresaId);
                return (product.Id, variant.Id);
            }

            var ownA = await SeedAsync(a.Id, "A-simple", true);
            var ownB = await SeedAsync(b.Id, "B-commercial", false);
            var rollback = await SeedAsync(a.Id, "A-rollback", true);
            var withStock = await SeedAsync(a.Id, "A-stock", false, 3);
            // Product and RBAC services/repositories are real. Mocked dependencies
            // belong only to other (uninvoked) CRUD methods of the controller.
            // The multiempresa quarantine is OFF by default. This in-memory
            // test-only pilot flag is deliberately never deployed to environments.
            var pilotConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateProductDeletePilotEnabled"] = "false"
                })
                .Build();
            using var host = await new HostBuilder()
                .ConfigureLogging(log => log.AddConsole())
                .ConfigureWebHost(web => web.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<AppDbContext>(o =>
                            o.UseMySQL(isolated, mysql => mysql.MigrationsAssembly(
                                "Solqaryn.Infrastructure.Migrations")));
                        services.AddHttpContextAccessor();
                        // Reflection-based UseMiddleware resolves InvokeAsync
                        // parameters from DI even for a path not using public store.
                        // These inert public-host collaborators are not catalog
                        // authorization bypasses and are never called by DELETE /productos.
                        services.AddSingleton(Mock.Of<IPublicStoreHostTenantResolver>());
                        services.AddSingleton(Mock.Of<IPublicStoreProofReplayStore>());
                        services.AddSingleton<IConfiguration>(pilotConfig);
                        services.AddScoped<ICurrentUserService, CurrentUserService>();
                        services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                        services.AddScoped<IRolPermisoRepository, RolPermisoRepository>();
                        services.AddScoped<IRolRepository, RolRepository>();
                        services.AddScoped<IPermisoRepository, PermisoRepository>();
                        services.AddScoped<IPermisoService, PermisoService>();
                        services.AddScoped<IProductoRepository, ProductoRepository>();
                        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
                        services.AddScoped<ICategoriaService, CategoriaService>();
                        services.AddScoped<IImpuestoRepository, ImpuestoRepository>();
                        services.AddScoped<IImpuestoService, ImpuestoService>();
                        services.AddScoped<IProductoService, ProductoService>();
                        // Real technical variant and durable audit share the EF transaction.
                        services.AddScoped<IProductoVarianteRepository, ProductoVarianteRepository>();
                        services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
                        services.AddScoped<ICatalogoProductoService, CatalogoProductoService>();
                        services.AddScoped<IProductoVarianteService, ProductoVarianteService>();
                        services.AddScoped<IUnitOfWork, UnitOfWork>();
                        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
                        services.AddScoped<IAuditoriaService, AuditoriaService>();
                        services.AddSingleton(Mock.Of<IImageStorageService>());
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(o => o.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = Issuer,
                                    ValidAudience = Audience,
                                    IssuerSigningKey = Key,
                                    ClockSkew = TimeSpan.Zero
                                });
                        services.AddAuthorization();
                        services.AddControllers().AddApplicationPart(
                            typeof(ProductosController).Assembly);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMiddleware<ExceptionHandlingMiddleware>();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseMiddleware<UnscopedCommercialTenantGuardMiddleware>();
                        app.UseEndpoints(routes => routes.MapControllers());
                    }))
                .StartAsync();
            using var client = host.GetTestClient();

            var jwtA = Sign(userA.Id, roleA.Id);
            var jwtB = Sign(userB.Id, roleB.Id);

            async Task DeleteAsync(int product, int selectedTenant, string? token,
                HttpStatusCode expected, bool sqlFailure = false)
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, $"/productos/{product}");
                request.Headers.TryAddWithoutValidation("X-Empresa-Id", selectedTenant.ToString());
                if (token is not null)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request);
                Assert.Equal(expected, response.StatusCode);
                var body = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("C14_AUDIT_FAILURE", body);
                if (sqlFailure) Assert.Contains("No fue posible guardar", body);
            }

            async Task ReadbackAsync((int Product, int Variant) target, int tenant,
                bool deleted, int? actor = null, int stock = 0)
            {
                await using var read = new AppDbContext(options);
                var product = await read.Productos.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(x => x.Id == target.Product);
                var variant = await read.ProductoVariantes.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(x => x.Id == target.Variant);
                Assert.Equal(tenant, product.EmpresaId);
                Assert.Equal(tenant, variant.EmpresaId);
                Assert.Equal(deleted, product.Eliminado);
                Assert.Equal(deleted, variant.Eliminado);
                Assert.Equal(!deleted, product.Activo);
                Assert.Equal(!deleted, variant.Activo);
                Assert.Equal(stock, variant.Cantidad);
                Assert.Equal(actor, product.EliminadoPorUsuarioId);
                Assert.Equal(actor, variant.EliminadoPorUsuarioId);
                var audits = await read.RegistrosAuditoria.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Modulo == ModuloSistema.Productos &&
                        x.Accion == AccionPermiso.EliminarLogico &&
                        x.Entidad == "Producto" && x.ReferenciaId == target.Product)
                    .ToListAsync();
                if (deleted)
                {
                    var audit = Assert.Single(audits);
                    Assert.Equal(tenant, audit.EmpresaId);
                    Assert.Equal(actor, audit.UsuarioId);
                }
                else Assert.Empty(audits);
            }

            await DeleteAsync(ownA.Product, a.Id, null, HttpStatusCode.Unauthorized);
            await DeleteAsync(ownA.Product, a.Id, jwtA, HttpStatusCode.ServiceUnavailable);
            pilotConfig["MultiEmpresa:PrivateProductDeletePilotEnabled"] = "true";
            await DeleteAsync(ownA.Product, a.Id, jwtB, HttpStatusCode.ServiceUnavailable);
            await DeleteAsync(ownA.Product, b.Id, jwtB, HttpStatusCode.NotFound);
            await DeleteAsync(ownB.Product, a.Id, jwtA, HttpStatusCode.NotFound);
            await ReadbackAsync(ownA, a.Id, false);
            await ReadbackAsync(ownB, b.Id, false);

            var grantA = await db.RolPermisos.SingleAsync(x =>
                x.RolId == roleA.Id && x.PermisoId == permission.Id);
            db.RolPermisos.Remove(grantA);
            await db.SaveChangesAsync();
            await DeleteAsync(ownA.Product, a.Id, jwtA, HttpStatusCode.ServiceUnavailable);
            await ReadbackAsync(ownA, a.Id, false);
            db.RolPermisos.Add(new RolPermiso { RolId = roleA.Id, PermisoId = permission.Id });
            await db.SaveChangesAsync();

            await DeleteAsync(withStock.Product, a.Id, jwtA, HttpStatusCode.BadRequest);
            await ReadbackAsync(withStock, a.Id, false, stock: 3);
            await DeleteAsync(ownA.Product, a.Id, jwtA, HttpStatusCode.OK);
            await ReadbackAsync(ownA, a.Id, true, userA.Id);
            await ReadbackAsync(ownB, b.Id, false);
            await DeleteAsync(ownA.Product, a.Id, jwtA, HttpStatusCode.NotFound);
            await ReadbackAsync(ownA, a.Id, true, userA.Id);
            await DeleteAsync(ownB.Product, b.Id, jwtB, HttpStatusCode.OK);
            await ReadbackAsync(ownB, b.Id, true, userB.Id);

            // Cause a real database failure at the durable audit INSERT, after
            // the product/variant UPDATEs. This is not a mocked UnitOfWork.
            var table = db.Model.FindEntityType(typeof(RegistroAuditoria))!.GetTableName();
            Assert.Equal("RegistrosAuditoria", table);
            await using (var sql = new MySqlConnection(isolated))
            {
                await sql.OpenAsync();
                await using var trigger = sql.CreateCommand();
                trigger.CommandText = "CREATE TRIGGER c14_delete_audit_failure BEFORE INSERT ON RegistrosAuditoria " +
                    "FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'C14_AUDIT_FAILURE'";
                await trigger.ExecuteNonQueryAsync();
                try
                {
                    await DeleteAsync(rollback.Product, a.Id, jwtA, HttpStatusCode.ServiceUnavailable, sqlFailure: true);
                    await ReadbackAsync(rollback, a.Id, false);
                    await ReadbackAsync(ownB, b.Id, true, userB.Id);
                }
                finally
                {
                    trigger.CommandText = "DROP TRIGGER c14_delete_audit_failure";
                    await trigger.ExecuteNonQueryAsync();
                }
            }
            // Same request succeeds once the SQL failure has been removed.
            await DeleteAsync(rollback.Product, a.Id, jwtA, HttpStatusCode.OK);
            await ReadbackAsync(rollback, a.Id, true, userA.Id);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static Usuario NewUser(string suffix, int roleId) => new()
    {
        NombreUsuario = "c1402_m0606_" + suffix + "_" + Guid.NewGuid().ToString("N")[..9],
        NombreCompleto = "C1402 isolated actor " + suffix,
        PasswordHash = "not-a-real-password",
        RolId = roleId, Activo = true
    };

    private static string Sign(int userId, int roleId,
        string? issuer = null, string? audience = null,
        DateTime? notBefore = null, DateTime? expires = null)
    {
        var claims = new[]
        {
            new Claim("id", userId.ToString()),
            new Claim("rolId", roleId.ToString()),
            new Claim("esAdministrador", "false")
        };
        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer, audience: audience ?? Audience, claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            expires: expires ?? DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
