using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Security;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>Ruta solo dentro del assembly de pruebas, no API de produccion.</summary>
[ApiController]
[Authorize]
[Route("c0402-m0306-test/{empresaId:int}")]
public sealed class C0402M0306TestOnlyController(
    C0402ContractAuditedPreflight audited) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int empresaId, CancellationToken ct)
    {
        var intent = new C0402ContractApplicationService.ClientIntent(
            7001, empresaId, 2, Rules.ContractOperation.Suspend,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            "private-contract-cause-must-not-persist");
        var decision = await audited.PrepareAuditedAsync(intent, ct);
        var result = new {
            decision.Code, decision.IsDomainCandidateOnly,
            permitsContractMutation = decision.AuthorizesContractMutation,
            permitsPaymentOrRefund = decision.AuthorizesPaymentOrRefund
        };
        return decision.IsDomainCandidateOnly
            ? Ok(result)
            : StatusCode(StatusCodes.Status403Forbidden, result);
    }
}

[Trait("Category", "Integration")]
public sealed class C0402M0306SignedJwtAuditMysql84HttpIntegrationTests
{
    private const string Issuer = "c0402-m0306-ci-issuer";
    private const string Audience = "c0402-m0306-ci-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0402-m0306-ephemeral-jwt-integration-key-only-2026"));

    private sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUserService
    {
        public bool EstaAutenticado => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
        public int? UsuarioId => int.TryParse(
            accessor.HttpContext?.User.FindFirst("id")?.Value, out var id) ? id : null;
        public string? NombreUsuario => null;
        public string? NombreCompleto => null;
        public int? RolId => 999999;
        public bool EsAdministrador => true;
    }

    // Only the contract snapshot is synthetic; JWT signature, HTTP, actor
    // membership, RBAC revocation and audit COMMIT all use real test services.
    // This does not certify a live contract repository or production DI.
    private sealed class TestOnlyOwnedContract(int tenantId)
        : IC0402VerifiedContractSnapshotSource
    {
        public ValueTask<C0402ContractApplicationService.VerifiedContractContext?> ReadOwnedAsync(
            int authenticatedTenantId, long contractIdHint, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (authenticatedTenantId != tenantId || contractIdHint != 7001)
                return ValueTask.FromResult<
                    C0402ContractApplicationService.VerifiedContractContext?>(null);
            return ValueTask.FromResult<
                C0402ContractApplicationService.VerifiedContractContext?>(
                new(new Rules.ContractSnapshot(tenantId, 7001,
                        Rules.ContractState.Active, 2,
                        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null),
                    new Rules.CommercialEvidence(true, true, true, true, true, true, true),
                    true, true, true, true));
        }
    }

    [Fact]
    public async Task SignedJwtMysqlTenantABAndRoleRevocationFailClosed()
    {
        var raw = Environment.GetEnvironmentVariable("C0402_M0306_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0402_m0306_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, m => m.MigrationsAssembly(
                "Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            await using (var sql = db.Database.GetDbConnection().CreateCommand())
            {
                sql.CommandText = "SELECT VERSION()";
                var version = Convert.ToString(await sql.ExecuteScalarAsync());
                Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true);
            }
            await db.Database.CloseConnectionAsync();

            var a = new Empresa("C0402M0306 Tenant A");
            var b = new Empresa("C0402M0306 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0402M0306 role A", NombreNormalizado = "C0402_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0402M0306 role B", NombreNormalizado = "C0402_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var user = new Usuario
            {
                NombreUsuario = "c0402_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "C0402 isolated actor",
                PasswordHash = "not-a-real-password",
                RolId = roleA.Id, Activo = true
            };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.Add(new UsuarioEmpresa(user.Id, a.Id, roleA.Id));
            await db.SaveChangesAsync();
            async Task<Permiso> RequirePreflightGrantAsync(
                ModuloSistema module, string code)
            {
                var permission = await db.Permisos.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p =>
                        p.Modulo == module &&
                        p.Accion == AccionPermiso.Ver &&
                        p.Ambito == AmbitoAutorizacion.Empresa);
                if (permission is null)
                {
                    permission = new Permiso
                    {
                        Codigo = code,
                        Nombre = code,
                        Modulo = module,
                        Accion = AccionPermiso.Ver,
                        Ambito = AmbitoAutorizacion.Empresa,
                        Activo = true
                    };
                    db.Permisos.Add(permission);
                    await db.SaveChangesAsync();
                }
                Assert.True(permission.Activo && !permission.Eliminado);
                db.RolPermisos.Add(new RolPermiso
                {
                    RolId = roleA.Id, PermisoId = permission.Id
                });
                await db.SaveChangesAsync();
                return permission;
            }
            await RequirePreflightGrantAsync(
                ModuloSistema.Facturacion, "C0402M0306.FACTURACION.VER");
            var configPermission = await RequirePreflightGrantAsync(
                ModuloSistema.Configuracion, "C0402M0306.CONFIGURACION.VER");
            db.ChangeTracker.Clear();
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE V23C0402ContractAuditIsolated (
                    EventId CHAR(32) NOT NULL PRIMARY KEY,
                    EventCode VARCHAR(64) NOT NULL,
                    OutcomeCode VARCHAR(96) NOT NULL,
                    IsDomainCandidateOnly BOOLEAN NOT NULL,
                    OccurredUtc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB
                """);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ContractAuthorization:EnableReadOnlyActorProbe"] = "true"
                }).Build();
            using var host = await new HostBuilder()
                .ConfigureWebHost(web => web.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IConfiguration>(config);
                        services.AddDbContext<AppDbContext>(o =>
                            o.UseMySQL(isolated, m => m.MigrationsAssembly(
                                "Solqaryn.Infrastructure.Migrations")));
                        services.AddHttpContextAccessor();
                        services.AddScoped<ICurrentUserService, HttpCurrentUser>();
                        services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                        services.AddScoped<IC0402VerifiedContractActorSource,
                            C0402ContractVerifiedActorSource>();
                        services.AddScoped<IC0402VerifiedContractSnapshotSource>(
                            _ => new TestOnlyOwnedContract(a.Id));
                        services.AddScoped<C0402ContractApplicationService>();
                        services.AddScoped<C0402ContractAuditedPreflight>();
                        services.AddScoped<IC0402ContractAuditSink>(_ =>
                            new C0402IsolatedMySqlContractAuditSink(
                                () => new MySqlConnection(isolated)));
                        services.AddLogging(l => l.SetMinimumLevel(LogLevel.Information));
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
                            typeof(C0402M0306TestOnlyController).Assembly);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(routes => routes.MapControllers());
                    }))
                .StartAsync();
            using var client = host.GetTestClient();

            using (var anonymous = await client.GetAsync(
                $"/c0402-m0306-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

            var signed = Sign(user.Id);
            var parts = signed.Split('.');
            parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join(".", parts));
            using (var forged = await client.GetAsync(
                $"/c0402-m0306-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", signed);
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Empresa-Id", b.Id.ToString());
            using (var allowed = await client.GetAsync(
                $"/c0402-m0306-test/{a.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Contains(a.Id.ToString(),
                    await allowed.Content.ReadAsStringAsync(),
                    StringComparison.Ordinal);
            }
            using (var foreign = await client.GetAsync(
                $"/c0402-m0306-test/{b.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);

            var grant = await db.RolPermisos.IgnoreQueryFilters()
                .SingleAsync(p => p.RolId == roleA.Id &&
                                  p.PermisoId == configPermission.Id);
            db.RolPermisos.Remove(grant);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            using (var revoked = await client.GetAsync(
                $"/c0402-m0306-test/{a.Id}"))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
            // Only the three authenticated calls reached the audit boundary:
            // positive tenant A, foreign tenant B and post-revocation denial.
            await using (var auditConnection = new MySqlConnection(isolated))
            {
                await auditConnection.OpenAsync();
                await using var auditQuery = auditConnection.CreateCommand();
                auditQuery.CommandText = """
                    SELECT COUNT(*), SUM(IsDomainCandidateOnly),
                           GROUP_CONCAT(OutcomeCode ORDER BY IsDomainCandidateOnly DESC)
                    FROM V23C0402ContractAuditIsolated
                    """;
                await using var auditRows = await auditQuery.ExecuteReaderAsync();
                Assert.True(await auditRows.ReadAsync());
                Assert.Equal(3L, Convert.ToInt64(auditRows.GetValue(0)));
                Assert.Equal(1L, Convert.ToInt64(auditRows.GetValue(1)));
                var outcomes = auditRows.GetString(2);
                Assert.Contains("REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED", outcomes);
                Assert.Contains("CLIENT_TENANT_HINT_MISMATCH", outcomes);
                Assert.Contains("LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED", outcomes);
                Assert.DoesNotContain("private-contract-cause", outcomes);
                Assert.False(await auditRows.ReadAsync());
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static string Sign(int id)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims:
            [
                new Claim("id", id.ToString()),
                new Claim("rolId", "999999"),
                new Claim("esAdministrador", "true")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
