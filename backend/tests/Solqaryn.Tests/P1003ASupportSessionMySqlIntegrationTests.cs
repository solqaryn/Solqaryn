using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class P1003ASupportSessionMySqlIntegrationTests
{
    [Fact]
    public async Task Temporary_support_session_is_scoped_read_only_audited_and_revocable()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero de CI no está configurado.");
        var database = "p1003a_" + Guid.NewGuid().ToString("N");
        var connectionString = new MySqlConnectionStringBuilder(raw)
        {
            Database = database
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connectionString,
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .AddInterceptors(new SupportReadOnlyCommandInterceptor())
            .Options;

        try
        {
            int operatorId;
            int empresaAId;
            int empresaBId;
            int productoAId = 0;
            int productoBId = 0;
            var username = "p1003a-operator-" + Guid.NewGuid().ToString("N");

            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);

                var empresaA = new Empresa("P1003A A " + Guid.NewGuid().ToString("N"));
                var empresaB = new Empresa("P1003A B " + Guid.NewGuid().ToString("N"));
                setup.Set<Empresa>().AddRange(empresaA, empresaB);
                await setup.SaveChangesAsync();
                empresaAId = empresaA.Id;
                empresaBId = empresaB.Id;

                var platformRole = new Rol
                {
                    Nombre = "P1003A operador plataforma",
                    NombreNormalizado = "P1003A_OPERADOR_" + Guid.NewGuid().ToString("N"),
                    EsSistema = true,
                    EsAdministrador = true,
                    Ambito = AmbitoAutorizacion.Plataforma,
                    Activo = true
                };
                setup.Roles.Add(platformRole);
                await setup.SaveChangesAsync();

                var user = new Usuario
                {
                    NombreUsuario = username,
                    NombreCompleto = "P1003A operador de prueba",
                    PasswordHash = "hash-efimero-no-utilizable",
                    RolId = platformRole.Id,
                    Activo = true
                };
                setup.Usuarios.Add(user);
                await setup.SaveChangesAsync();
                operatorId = user.Id;
                setup.Set<UsuarioRolPlataforma>().Add(
                    new UsuarioRolPlataforma(operatorId, platformRole.Id));
                await setup.SaveChangesAsync();

                Assert.Empty(await setup.UsuarioEmpresas.IgnoreQueryFilters()
                    .Where(x => x.UsuarioId == operatorId).ToListAsync());
            }

            await SeedProductAsync(options, empresaAId, "Producto de prueba A", id => productoAId = id);
            await SeedProductAsync(options, empresaBId, "Producto de prueba B", id => productoBId = id);

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UsuarioId).Returns(operatorId);
            currentUser.SetupGet(x => x.NombreUsuario).Returns(username);
            currentUser.SetupGet(x => x.EstaAutenticado).Returns(true);

            var creationHttp = AuthenticatedRequest(HttpMethods.Post, operatorId, username);
            int sessionId;
            string token;
            await using (var db = NewDb(options, creationHttp))
            {
                var created = await NewService(db, currentUser.Object, options).CreateAsync(
                    empresaAId,
                    ModuloSistema.Productos,
                    "P1-003A soporte de prueba",
                    15,
                    Audit("grant"));

                Assert.True(created.Succeeded, created.Code);
                Assert.NotNull(created.Token);
                Assert.Equal(43, created.Token!.Length);

                var stored = await db.Set<PlataformaSesionSoporte>().IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(x => x.Id == created.SessionId);
                Assert.Equal(64, stored.TokenHash.Length);
                Assert.NotEqual(created.Token, stored.TokenHash);
                Assert.Equal(operatorId, stored.OperadorUsuarioId);
                Assert.Equal(empresaAId, stored.EmpresaId);
                Assert.Null(stored.RevocadaUtc);

                token = created.Token;
                sessionId = created.SessionId!.Value;
            }

            var wrongCompanyHttp = AuthenticatedRequest(HttpMethods.Get, operatorId, username);
            await using (var dbWrongCompany = NewDb(options, wrongCompanyHttp))
            {
                var wrong = await NewService(dbWrongCompany, currentUser.Object, options)
                    .AuthorizeReadAsync(token, empresaBId, ModuloSistema.Productos,
                        routeIsExplicitRead: true, Audit("wrong-company"));
                Assert.False(wrong.Authorized);
                Assert.Equal(403, wrong.StatusCode);
            }

            var wrongRouteHttp = AuthenticatedRequest(HttpMethods.Get, operatorId, username);
            await using (var dbWrongRoute = NewDb(options, wrongRouteHttp))
            {
                var wrong = await NewService(dbWrongRoute, currentUser.Object, options)
                    .AuthorizeReadAsync(token, empresaAId, ModuloSistema.Productos,
                        routeIsExplicitRead: false, Audit("wrong-route"));
                Assert.False(wrong.Authorized);
                Assert.Equal("EXPLICIT_READ_SCOPE_REQUIRED", wrong.Code);
            }

            var validHttp = AuthenticatedRequest(HttpMethods.Get, operatorId, username);
            await using (var dbRead = NewDb(options, validHttp))
            {
                var validService = NewService(dbRead, currentUser.Object, options);
                var valid = await validService.AuthorizeReadAsync(
                    token, empresaAId, ModuloSistema.Productos,
                    routeIsExplicitRead: true, Audit("valid-read"));
                Assert.True(valid.Authorized, valid.Code);

                var scope = new VerifiedPlatformSupportTenant.Scope(
                    valid.SessionId!.Value, empresaAId, ModuloSistema.Productos);
                VerifiedPlatformSupportTenant.MarkCandidate(validHttp, scope);
                VerifiedPlatformSupportTenant.MarkAuthorized(validHttp, scope);

                var products = await dbRead.Productos.AsNoTracking().ToListAsync();
                var product = Assert.Single(products);
                Assert.Equal(productAId, product.Id);
                Assert.DoesNotContain(products, x => x.Id == productoBId);

                var tracked = await dbRead.Productos.SingleAsync(x => x.Id == productoAId);
                tracked.Nombre = "Intento de escritura por soporte";
                await Assert.ThrowsAsync<Solqaryn.Application.Exceptions.ForbiddenAccessException>(
                    () => dbRead.SaveChangesAsync());
                dbRead.Entry(tracked).State = EntityState.Unchanged;

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    dbRead.Database.ExecuteSqlRawAsync(
                        "UPDATE Productos SET Nombre = 'bloqueado' WHERE Id = " +
                        productoAId.ToString(System.Globalization.CultureInfo.InvariantCulture)));

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    dbRead.Database.ExecuteSqlRawAsync(
                        "SELECT Id FROM Productos FOR SHARE"));

                await validService.CompleteAuthorizedReadAsync(
                    requestCompleted: true, CancellationToken.None);
                Assert.Null(dbRead.Database.CurrentTransaction);
            }

            // La autoridad de plataforma se relee en cada solicitud. Retirar
            // el grant existente invalida la sesión sin tocar membresías tenant.
            await using (var authorityDb = new AppDbContext(options))
            {
                var assignment = await authorityDb.Set<UsuarioRolPlataforma>()
                    .IgnoreQueryFilters().SingleAsync(x => x.UsuarioId == operatorId);
                assignment.Desactivar();
                await authorityDb.SaveChangesAsync();
            }

            var revokedAuthorityHttp = AuthenticatedRequest(HttpMethods.Get, operatorId, username);
            await using (var dbRevokedAuthority = NewDb(options, revokedAuthorityHttp))
            {
                var denied = await NewService(dbRevokedAuthority, currentUser.Object, options)
                    .AuthorizeReadAsync(token, empresaAId, ModuloSistema.Productos,
                        routeIsExplicitRead: true, Audit("authority-revoked"));
                Assert.False(denied.Authorized);
                Assert.Equal("PLATFORM_AUTHORITY_REQUIRED", denied.Code);
            }

            await using (var authorityDb = new AppDbContext(options))
            {
                var assignment = await authorityDb.Set<UsuarioRolPlataforma>()
                    .IgnoreQueryFilters().SingleAsync(x => x.UsuarioId == operatorId);
                assignment.Activar();
                await authorityDb.SaveChangesAsync();
            }

            // El estado activo de la empresa también se comprueba en cada uso.
            await using (var companyDb = new AppDbContext(options))
            {
                var company = await companyDb.Set<Empresa>().IgnoreQueryFilters()
                    .SingleAsync(x => x.Id == empresaAId);
                company.Desactivar();
                await companyDb.SaveChangesAsync();
            }

            var inactiveCompanyHttp = AuthenticatedRequest(HttpMethods.Get, operatorId, username);
            await using (var dbInactiveCompany = NewDb(options, inactiveCompanyHttp))
            {
                var denied = await NewService(dbInactiveCompany, currentUser.Object, options)
                    .AuthorizeReadAsync(token, empresaAId, ModuloSistema.Productos,
                        routeIsExplicitRead: true, Audit("company-inactive"));
                Assert.False(denied.Authorized);
                Assert.Equal(403, denied.StatusCode);
            }

            await using (var companyDb = new AppDbContext(options))
            {
                var company = await companyDb.Set<Empresa>().IgnoreQueryFilters()
                    .SingleAsync(x => x.Id == empresaAId);
                company.Activar();
                await companyDb.SaveChangesAsync();
            }

            // The active request keeps the session row locked through the full
            // authorized read. A concurrent revoke must wait, then win before replay.
            var heldReadHttp = AuthenticatedRequest(HttpMethods.Get, operatorId, username);
            await using (var dbHeldRead = NewDb(options, heldReadHttp))
            {
                var heldReadService = NewService(dbHeldRead, currentUser.Object, options);
                var heldRead = await heldReadService.AuthorizeReadAsync(
                    token, empresaAId, ModuloSistema.Productos,
                    routeIsExplicitRead: true, Audit("concurrent-read"));
                Assert.True(heldRead.Authorized, heldRead.Code);
                Assert.NotNull(dbHeldRead.Database.CurrentTransaction);

                var revokeStarted = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                var revokeTask = Task.Run(async () =>
                {
                    var concurrentHttp = AuthenticatedRequest(
                        HttpMethods.Delete, operatorId, username);
                    await using var dbConcurrentRevoke = NewDb(options, concurrentHttp);
                    var revokeService = NewService(
                        dbConcurrentRevoke, currentUser.Object, options);
                    revokeStarted.SetResult();
                    return await revokeService.RevokeAsync(
                        sessionId, Audit("concurrent-revoke"));
                });

                await revokeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                Assert.False(revokeTask.IsCompleted,
                    "La revocación adelantó a una lectura que aún conserva el lock de sesión.");

                await heldReadService.CompleteAuthorizedReadAsync(
                    requestCompleted: true, CancellationToken.None);
                var revoked = await revokeTask.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.True(revoked.Succeeded, revoked.Code);
            }

            var replayHttp = AuthenticatedRequest(HttpMethods.Get, operatorId, username);
            await using (var dbReplay = NewDb(options, replayHttp))
            {
                var replay = await NewService(dbReplay, currentUser.Object, options)
                    .AuthorizeReadAsync(token, empresaAId, ModuloSistema.Productos,
                        routeIsExplicitRead: true, Audit("after-revoke"));
                Assert.False(replay.Authorized);
                Assert.Equal(403, replay.StatusCode);
            }

            await using (var auditDb = new AppDbContext(options))
            {
                var events = await auditDb.Set<RegistroAuditoria>().AsNoTracking()
                    .Where(x => x.Entidad == nameof(PlataformaSesionSoporte))
                    .ToListAsync();
                Assert.Contains(events, x => x.Accion == AccionPermiso.Crear &&
                    x.Resultado == "Exito");
                Assert.Contains(events, x => x.Accion == AccionPermiso.Ver &&
                    x.Resultado == "Exito");
                Assert.Contains(events, x => x.Resultado == "Denegado");
                Assert.Contains(events, x => x.Accion == AccionPermiso.Desactivar &&
                    x.Resultado == "Exito");
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static async Task SeedProductAsync(
        DbContextOptions<AppDbContext> options,
        int empresaId,
        string name,
        Action<int> saveId)
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
        await using var db = NewDb(options, http);
        var product = new Producto
        {
            Nombre = name,
            Costo = 25m,
            Precio = 40m,
            Cantidad = 1,
            Activo = true
        };
        db.Productos.Add(product);
        await db.SaveChangesAsync();
        Assert.Equal(empresaId, product.EmpresaId);
        saveId(product.Id);
    }

    private static AppDbContext NewDb(
        DbContextOptions<AppDbContext> options,
        HttpContext? http = null) =>
        new(options, tenantHttpContextAccessor: http is null
            ? null
            : new HttpContextAccessor { HttpContext = http });

    private static PlataformaSoporteSessionService NewService(
        AppDbContext db,
        ICurrentUserService current,
        DbContextOptions<AppDbContext> options)
    {
        var platform = new PlataformaScopeService(db, current);
        return new PlataformaSoporteSessionService(db, current, platform, options);
    }

    private static DefaultHttpContext AuthenticatedRequest(string method, int userId, string username)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[]
                {
                    new System.Security.Claims.Claim("id", userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new System.Security.Claims.Claim("nombreUsuario", username)
                },
                authenticationType: "integration-test"));
        return http;
    }

    private static PlatformSupportAuditContext Audit(string suffix) =>
        new("127.0.0.1", "P1003A integration test", "p1003a-" + suffix);
}
