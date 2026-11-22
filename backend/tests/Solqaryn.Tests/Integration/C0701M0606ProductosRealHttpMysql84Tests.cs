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
/// C07-01.M06.06: endpoint de produccion /productos, JWT firmado y
/// RBAC/membresias reales en Oracle EF/MySQL 8.4 efimero.
/// Esta lectura no certifica Angular, CRUD, variantes ni rollback.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0701M0606ProductosRealHttpMysql84Tests
{
    private const string Issuer = "c0701-m0606-ephemeral-issuer";
    private const string Audience = "c0701-m0606-ephemeral-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0701-m0606-local-test-signing-key-isolated-2026"));

    [Fact]
    public async Task RealProductosGetUsesSignedJwtMysqlTenantABAndRevocation()
    {
        var raw = Environment.GetEnvironmentVariable("C0701_M0606_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root" ||
            origin.Database != "c0701_m0606_bootstrap")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0701_m0606_" + Guid.NewGuid().ToString("N")
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

            var a = new Empresa("C0701M0606 Tenant A");
            var b = new Empresa("C0701M0606 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0701M0606 role A", NombreNormalizado = "C0701M0606_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0701M0606 role B", NombreNormalizado = "C0701M0606_B",
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
                    p.Accion == AccionPermiso.Ver &&
                    p.Ambito == AmbitoAutorizacion.Empresa && p.Activo && !p.Eliminado);
            if (permission is null)
            {
                permission = new Permiso
                {
                    Codigo = "C0701M0606.PRODUCTOS.VER",
                    Nombre = "C0701 M0606 isolated permission",
                    Modulo = ModuloSistema.Productos, Accion = AccionPermiso.Ver,
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

            // Real tenant-owned products, seeded only using a server-side verified
            // commercial authority, never by trusting the request header.
            async Task<int> SeedOwnedProductAsync(int tenantId, string name)
            {
                var request = new DefaultHttpContext();
                request.Items[
                    Solqaryn.Application.Security.VerifiedCommercialTenant
                        .AuthorizedEmpresaIdItemKey] = tenantId;
                var accessor = new HttpContextAccessor { HttpContext = request };
                await using var owned = new AppDbContext(options,
                    tenantHttpContextAccessor: accessor);
                var category = new Categoria { Nombre = "Category " + name };
                owned.Categorias.Add(category);
                await owned.SaveChangesAsync();
                var product = new Producto
                {
                    Nombre = name, Marca = "", Modelo = "",
                    Precio = 100m, Costo = 50m, CategoriaId = category.Id
                };
                owned.Productos.Add(product);
                await owned.SaveChangesAsync();
                Assert.Equal(tenantId, product.EmpresaId);
                return product.Id;
            }
            var productAId = await SeedOwnedProductAsync(a.Id, "C0701RealProductoA");
            var productBId = await SeedOwnedProductAsync(b.Id, "C0701RealProductoB");

            // P1: una escritura de producto no puede sobrevivir si la bitacora
            // critica no se persiste. Unidad de trabajo REAL + Oracle EF/MySQL.
            // Esto prueba rollback de ProductService.CreateAsync, no habilita
            // escrituras HTTP comerciales ni certifica UI/variantes.
            var auditRollbackName = "C0701AuditRollback" + Guid.NewGuid().ToString("N");
            var auditContext = new DefaultHttpContext();
            auditContext.Items[
                Solqaryn.Application.Security.VerifiedCommercialTenant
                    .AuthorizedEmpresaIdItemKey] = a.Id;
            var auditAccessor = new HttpContextAccessor { HttpContext = auditContext };
            await using (var auditDb = new AppDbContext(options,
                tenantHttpContextAccessor: auditAccessor))
            {
                var categoryId = await auditDb.Categorias
                    .Where(c => c.Nombre == "Category C0701RealProductoA")
                    .Select(c => c.Id).SingleAsync();
                var actor = new Mock<ICurrentUserService>();
                actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                var unavailableAudit = new Mock<IAuditoriaService>();
                unavailableAudit.Setup(x => x.RegistrarEstrictoAsync(
                        It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                        It.IsAny<string>(), It.IsAny<int?>(),
                        It.IsAny<string?>(), It.IsAny<object?>(),
                        It.IsAny<object?>(), It.IsAny<string?>(),
                        It.IsAny<string>(), It.IsAny<string?>()))
                    .ThrowsAsync(new InvalidOperationException("AUDIT_PERSISTENCE_UNAVAILABLE"));
                var service = new ProductoService(
                    new ProductoRepository(auditDb),
                    new CategoriaRepository(auditDb),
                    Mock.Of<IImageStorageService>(), actor.Object,
                    unavailableAudit.Object, httpContextAccessor: auditAccessor);
                var uow = new UnitOfWork(auditDb);
                var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => uow.ExecuteInTransactionAsync(async () =>
                    {
                        await service.CreateAsync(new CreateProductoDto
                        {
                            Nombre = auditRollbackName,
                            CategoriaId = categoryId
                        });
                    }));
                Assert.Equal("AUDIT_PERSISTENCE_UNAVAILABLE", thrown.Message);
                unavailableAudit.Verify(x => x.RegistrarEstrictoAsync(
                    ModuloSistema.Productos, AccionPermiso.Crear,
                    It.IsAny<string>(), It.IsAny<int?>(),
                    It.IsAny<string?>(), It.IsAny<object?>(),
                    It.IsAny<object?>(), It.IsAny<string?>(),
                    It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
            }
            await using (var auditReadback = new AppDbContext(options))
            {
                Assert.Equal(0, await auditReadback.Productos
                    .IgnoreQueryFilters().AsNoTracking()
                    .CountAsync(p => p.Nombre == auditRollbackName));
            }


            // Strict audit is required for every material product write,
            // not only Create. A failed injected audit must roll back the
            // actual Oracle EF/MySQL transaction in the same isolated schema.
            // This is a NEGATIVE real-DB proof, not a positive durable audit
            // or permission to bypass private HTTP write quarantine.
            async Task AssertStrictAuditRollbackAsync(
                AccionPermiso action, Func<ProductoService, Task> mutation)
            {
                await using (var changeDb = new AppDbContext(options,
                    tenantHttpContextAccessor: auditAccessor))
                {
                    var actor = new Mock<ICurrentUserService>();
                    actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                    actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                    var unavailableAudit = new Mock<IAuditoriaService>();
                    unavailableAudit.Setup(x => x.RegistrarEstrictoAsync(
                        ModuloSistema.Productos, action,
                            It.IsAny<string>(), It.IsAny<int?>(),
                            It.IsAny<string?>(), It.IsAny<object?>(),
                            It.IsAny<object?>(), It.IsAny<string?>(),
                            It.IsAny<string>(), It.IsAny<string?>()))
                        .ThrowsAsync(new InvalidOperationException(
                            "AUDIT_PERSISTENCE_UNAVAILABLE"));
                    var service = new ProductoService(
                        new ProductoRepository(changeDb),
                        new CategoriaRepository(changeDb),
                        Mock.Of<IImageStorageService>(), actor.Object,
                        unavailableAudit.Object, httpContextAccessor: auditAccessor);
                    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => new UnitOfWork(changeDb).ExecuteInTransactionAsync(
                            async () => await mutation(service)));
                    Assert.Equal("AUDIT_PERSISTENCE_UNAVAILABLE", failure.Message);
                    unavailableAudit.Verify(x => x.RegistrarEstrictoAsync(
                        ModuloSistema.Productos, action,
                        It.IsAny<string>(), It.IsAny<int?>(),
                        It.IsAny<string?>(), It.IsAny<object?>(),
                        It.IsAny<object?>(), It.IsAny<string?>(),
                        It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
                }
                await using (var readback = new AppDbContext(options))
                {
                    var untouched = await readback.Productos
                        .IgnoreQueryFilters().AsNoTracking()
                        .SingleAsync(p => p.Id == productAId);
                    Assert.Equal("C0701RealProductoA", untouched.Nombre);
                    Assert.True(untouched.Activo);
                    Assert.False(untouched.Eliminado);
                    Assert.Equal(100m, untouched.Precio);
                }
            }

            await AssertStrictAuditRollbackAsync(AccionPermiso.Editar,
                service => service.UpdateAsync(productAId,
                    new UpdateProductoDto
                    {
                        Nombre = "C0701AuditRejectedEdit" + Guid.NewGuid().ToString("N")
                    }));
            await AssertStrictAuditRollbackAsync(AccionPermiso.Desactivar,
                service => service.CambiarEstadoAsync(productAId, false));
            await AssertStrictAuditRollbackAsync(AccionPermiso.EliminarLogico,
                service => service.DeleteAsync(productAId));

            // Positive durable audit proof: unlike the injected-failure cases
            // above, use the real AuditoriaService/AuditoriaRepository and the
            // SAME Oracle EF DbContext/transaction as the product creation.
            // Reopen from a fresh context to prove both product and audit row
            // survive the commit and retain server-verified tenant ownership.
            // This does not certify the quarantined private HTTP write path.
            var auditedProductName = "C0701DurableAudit" + Guid.NewGuid().ToString("N");
            await using (var durableDb = new AppDbContext(options,
                tenantHttpContextAccessor: auditAccessor))
            {
                var categoryId = await durableDb.Categorias
                    .Where(c => c.Nombre == "Category C0701RealProductoA")
                    .Select(c => c.Id).SingleAsync();
                var actor = new Mock<ICurrentUserService>();
                actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                var audit = new AuditoriaService(
                    new AuditoriaRepository(durableDb), actor.Object,
                    auditAccessor,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditoriaService>.Instance);
                var service = new ProductoService(
                    new ProductoRepository(durableDb),
                    new CategoriaRepository(durableDb),
                    Mock.Of<IImageStorageService>(), actor.Object,
                    audit, httpContextAccessor: auditAccessor);
                await new UnitOfWork(durableDb).ExecuteInTransactionAsync(async () =>
                {
                    var created = await service.CreateAsync(new CreateProductoDto
                    {
                        Nombre = auditedProductName,
                        CategoriaId = categoryId
                    });
                    Assert.True(created.Id > 0);
                });
            }
            await using (var durableReadback = new AppDbContext(options))
            {
                var stored = await durableReadback.Productos
                    .IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(p => p.Nombre == auditedProductName);
                Assert.Equal(a.Id, stored.EmpresaId);
                var receipt = await durableReadback.RegistrosAuditoria
                    .IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(log => log.Modulo == ModuloSistema.Productos
                        && log.Accion == AccionPermiso.Crear
                        && log.ReferenciaId == stored.Id
                        && log.Entidad == "Producto");
                Assert.Equal(a.Id, receipt.EmpresaId);
                Assert.Equal(userA.Id, receipt.UsuarioId);
                Assert.Equal(userA.NombreUsuario, receipt.NombreUsuario);
                Assert.Equal("Exito", receipt.Resultado);
            }

            // Real Oracle EF/MySQL transaction rollback at the tenant-bound
            // persistence layer. This is NOT HTTP CRUD acceptance: writes
            // through ProductosController still require separate coverage.
            var rollbackName = "C0701RollbackProducto" + Guid.NewGuid().ToString("N");
            var rollbackSku = "C0701RollbackVariante" + Guid.NewGuid().ToString("N");
            var rollbackHttp = new DefaultHttpContext();
            rollbackHttp.Items[
                Solqaryn.Application.Security.VerifiedCommercialTenant
                    .AuthorizedEmpresaIdItemKey] = a.Id;
            var rollbackAccessor = new HttpContextAccessor { HttpContext = rollbackHttp };
            await using (var rollbackDb = new AppDbContext(options,
                tenantHttpContextAccessor: rollbackAccessor))
            {
                var categoryId = await rollbackDb.Categorias
                    .Where(c => c.Nombre == "Category C0701RealProductoA")
                    .Select(c => c.Id).SingleAsync();
                await using var transaction = await rollbackDb.Database.BeginTransactionAsync();
                var temporary = new Producto
                {
                    Nombre = rollbackName, Marca = "", Modelo = "",
                    Precio = 321m, Costo = 123m, CategoriaId = categoryId
                };
                rollbackDb.Productos.Add(temporary);
                await rollbackDb.SaveChangesAsync();
                Assert.Equal(a.Id, temporary.EmpresaId);
                Assert.Equal(1, await rollbackDb.Productos
                    .CountAsync(p => p.Nombre == rollbackName));

                // El rollback debe cubrir también modificaciones de catálogo
                // y una variante del mismo tenant, no solo el INSERT de Producto.
                var persistedA = await rollbackDb.Productos
                    .SingleAsync(p => p.Id == productAId);
                persistedA.Precio = 654m;
                var temporaryVariant = new ProductoVariante
                {
                    ProductoId = temporary.Id,
                    EmpresaId = a.Id,
                    Sku = rollbackSku,
                    Cantidad = 1,
                    Precio = 87m,
                    Costo = 53m
                };
                rollbackDb.ProductoVariantes.Add(temporaryVariant);
                await rollbackDb.SaveChangesAsync();
                Assert.Equal(a.Id, temporaryVariant.EmpresaId);
                Assert.Equal(1, await rollbackDb.ProductoVariantes
                    .CountAsync(v => v.Sku == rollbackSku));
                Assert.Equal(654m, await rollbackDb.Productos
                    .Where(p => p.Id == productAId)
                    .Select(p => p.Precio).SingleAsync());
                await transaction.RollbackAsync();
            }
            await using (var verified = new AppDbContext(options,
                tenantHttpContextAccessor: rollbackAccessor))
            {
                Assert.Equal(0, await verified.Productos
                    .CountAsync(p => p.Nombre == rollbackName));
                Assert.Equal(0, await verified.ProductoVariantes
                    .CountAsync(v => v.Sku == rollbackSku));
                Assert.Equal(100m, await verified.Productos
                    .Where(p => p.Id == productAId)
                    .Select(p => p.Precio).SingleAsync());
            }



            // Product and RBAC services/repositories are real. Mocked dependencies
            // belong only to other (uninvoked) CRUD methods of the controller.
            // The multiempresa quarantine is OFF by default. This in-memory
            // test-only pilot flag is deliberately never deployed to environments.
            var pilotConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false"
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
                        // authorization bypasses and are never called by GET /productos.
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
                        services.AddScoped<IProductoService, ProductoService>();
                        services.AddSingleton(Mock.Of<IProductoVarianteService>());
                        services.AddSingleton(Mock.Of<IUnitOfWork>());
                        services.AddSingleton(Mock.Of<IImageStorageService>());
                        services.AddSingleton(Mock.Of<IAuditoriaService>());
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

            using (var anonymous = await client.GetAsync("/productos"))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

            var jwtA = Sign(userA.Id, roleA.Id);
            var segments = jwtA.Split('.');
            segments[2] = (segments[2][0] == 'A' ? 'B' : 'A') + segments[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join('.', segments));
            using (var forged = await client.GetAsync("/productos"))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);

            // Production quarantine must reject even an authorized JWT until
            // the explicit server-side pilot flag is enabled.
            Assert.Equal(HttpStatusCode.ServiceUnavailable,
                await ReadStatusAsync(client, a.Id, jwtA));
            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "true";
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, a.Id, jwtA));

            // Physical product rows in the same MySQL schema must remain isolated
            // by the real controller, RBAC filter, service and EF query filter.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwtA);
            client.DefaultRequestHeaders.Remove("X-Empresa-Id");
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Empresa-Id", a.Id.ToString());
            using (var ownA = await client.GetAsync("/productos?page=1&pageSize=10"))
            {
                Assert.Equal(HttpStatusCode.OK, ownA.StatusCode);
                var json = await ownA.Content.ReadAsStringAsync();
                Assert.Contains("C0701RealProductoA", json, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701RealProductoB", json, StringComparison.Ordinal);
            }
            // Reales y adversariales: la seleccion de empresa nunca se infiere
            // de un JWT valido cuando el selector HTTP es ambiguo o invalido.
            // Mantener el request aislado evita contaminar cabeceras del cliente.
            async Task AssertRejectedTenantSelectorAsync(params string[] selectors)
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "/productos?page=1&pageSize=10");
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtA);
                request.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", selectors);
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                var rejectedBody = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("C0701RealProductoA", rejectedBody, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701RealProductoB", rejectedBody, StringComparison.Ordinal);
            }
            await AssertRejectedTenantSelectorAsync(a.Id.ToString(), b.Id.ToString());
            await AssertRejectedTenantSelectorAsync("0");
            await AssertRejectedTenantSelectorAsync("invalid-tenant-id");

            // A read-only tenant pilot must never activate commercial writes.
            // These are real signed-JWT HTTP requests through production routing
            // and tenant guard, followed by independent physical MySQL readback.
            var deniedName = "C0701ReadPilotDeniedWrite" + Guid.NewGuid().ToString("N");
            using (var createForm = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Nombre"] = deniedName,
                    ["Precio"] = "120",
                    ["Costo"] = "50"
                }))
            using (var deniedCreate = await client.PostAsync("/productos", createForm))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, deniedCreate.StatusCode);

            using (var updateForm = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Nombre"] = deniedName,
                    ["Precio"] = "120"
                }))
            using (var deniedForeignUpdate = await client.PutAsync(
                $"/productos/{productBId}", updateForm))
                Assert.Equal(HttpStatusCode.ServiceUnavailable,
                    deniedForeignUpdate.StatusCode);

            using (var deniedDelete = await client.DeleteAsync(
                $"/productos/{productAId}"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, deniedDelete.StatusCode);

            await using (var negativeReadback = new AppDbContext(options))
            {
                var persisted = await negativeReadback.Productos
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(p => p.Id == productAId || p.Id == productBId)
                    .Select(p => new { p.Id, p.Nombre, p.Eliminado, p.Precio })
                    .ToArrayAsync();
                Assert.Equal(2, persisted.Length);
                Assert.Contains(persisted, p => p.Id == productAId &&
                    p.Nombre == "C0701RealProductoA" && !p.Eliminado && p.Precio == 100m);
                Assert.Contains(persisted, p => p.Id == productBId &&
                    p.Nombre == "C0701RealProductoB" && !p.Eliminado && p.Precio == 100m);
                Assert.Equal(0, await negativeReadback.Productos
                    .IgnoreQueryFilters()
                    .CountAsync(p => p.Nombre == deniedName));
            }

            using (var foreignA = await client.GetAsync($"/productos/{productBId}"))
                Assert.Equal(HttpStatusCode.NotFound, foreignA.StatusCode);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, b.Id, jwtA));
            var jwtB = Sign(userB.Id, roleB.Id);
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, b.Id, jwtB));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwtB);
            client.DefaultRequestHeaders.Remove("X-Empresa-Id");
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Empresa-Id", b.Id.ToString());
            using (var ownB = await client.GetAsync("/productos?page=1&pageSize=10"))
            {
                Assert.Equal(HttpStatusCode.OK, ownB.StatusCode);
                var json = await ownB.Content.ReadAsStringAsync();
                Assert.Contains("C0701RealProductoB", json, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701RealProductoA", json, StringComparison.Ordinal);
            }
            using (var foreignB = await client.GetAsync($"/productos/{productAId}"))
                Assert.Equal(HttpStatusCode.NotFound, foreignB.StatusCode);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, a.Id, jwtB));

            var membershipA = await db.UsuarioEmpresas
                .SingleAsync(x => x.UsuarioId == userA.Id && x.EmpresaId == a.Id);
            membershipA.Desactivar();
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, a.Id, jwtA));
            membershipA.Activar();
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, a.Id, jwtA));

            var grantA = await db.RolPermisos
                .SingleAsync(x => x.RolId == roleA.Id && x.PermisoId == permission.Id);
            db.RolPermisos.Remove(grantA);
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, a.Id, jwtA));
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, b.Id, jwtB));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static Usuario NewUser(string suffix, int roleId) => new()
    {
        NombreUsuario = "c0701_m0606_" + suffix + "_" + Guid.NewGuid().ToString("N")[..9],
        NombreCompleto = "C0701 isolated actor " + suffix,
        PasswordHash = "not-a-real-password",
        RolId = roleId, Activo = true
    };

    private static async Task<HttpStatusCode> ReadStatusAsync(
        HttpClient client, int empresaId, string jwt)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);
        client.DefaultRequestHeaders.Remove("X-Empresa-Id");
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Empresa-Id", empresaId.ToString());
        using var response = await client.GetAsync("/productos?page=1&pageSize=10");
        return response.StatusCode;
    }

    private static string Sign(int userId, int roleId)
    {
        var claims = new[]
        {
            new Claim("id", userId.ToString()),
            new Claim("rolId", roleId.ToString()),
            new Claim("esAdministrador", "false")
        };
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: Audience, claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
