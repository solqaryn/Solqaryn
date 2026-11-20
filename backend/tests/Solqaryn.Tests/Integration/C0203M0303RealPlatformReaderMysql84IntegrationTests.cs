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
using Microsoft.IdentityModel.Tokens;
using MySql.EntityFrameworkCore.Extensions;
using MySqlConnector;
using Solqaryn.API.Security;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.C0203TenantLifecycleAuthorizationGate;
using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C02-03.M03.03: signed bearer JWT, real PlatformScopeService and real
/// C0203VerifiedPlatformOperatorEvidenceReader with Oracle EF on isolated
/// MySQL 8.4. Read-only TestServer; no production endpoint or tenant mutation.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0203M0303RealPlatformReaderMysql84IntegrationTests
{
    private const string BaseDb = "solqaryn_c1502_m0503";
    private const string Issuer = "solqaryn-c0203-real-reader-v22";
    private const string Audience = "solqaryn-c0203-real-reader-client";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0203-real-reader-isolated-test-signing-key-only-valid"));
    private static readonly SymmetricSecurityKey BadKey = new(
        Encoding.UTF8.GetBytes("c0203-real-reader-isolated-test-signing-key-forged"));

    private static string BaseConnection()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("MYSQL84_EPHEMERAL_CONNECTION_REQUIRED");
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server != "127.0.0.1" || parsed.Port != 3306 ||
            parsed.Database != BaseDb || parsed.UserID != "root" ||
            parsed.SslMode != MySqlSslMode.Disabled)
            throw new InvalidOperationException("MYSQL84_LOOPBACK_ONLY");
        return raw;
    }

    private static string Token(bool forged = false, bool wrongSubject = false) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub,
                    wrongSubject ? "other.user" : "platform.operator"),
                new Claim("nombreUsuario", "platform.operator"),
                new Claim("id", "911")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                forged ? BadKey : Key, SecurityAlgorithms.HmacSha256)));

    private static Task<IHost> StartAsync(string scopedConnection) =>
        new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddHttpContextAccessor();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true, ValidIssuer = Issuer,
                            ValidateAudience = true, ValidAudience = Audience,
                            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true, IssuerSigningKey = Key,
                            RequireSignedTokens = true, RequireExpirationTime = true
                        };
                    });
                services.AddAuthorization();
            }).Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapGet(
                    "/__c0203_real/{tenantId:int}", async http =>
                    {
                        var accessor = http.RequestServices
                            .GetRequiredService<IHttpContextAccessor>();
                        var current = new CurrentUserService(accessor);
                        var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseMySQL(scopedConnection).Options;
                        await using var db = new AppDbContext(options,
                            tenantHttpContextAccessor: accessor);
                        var platform = new PlataformaScopeService(db, current);
                        var config = new ConfigurationBuilder().AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                ["TenantLifecycle:EnableReadOnlyAuthorizationProbe"] = "true",
                                ["TenantLifecycle:AllowSuspendReadOnly"] = "true",
                                ["TenantLifecycle:ReadOnlyPolicyRevision"] = "1"
                            }).Build();
                        var reader = new C0203VerifiedPlatformOperatorEvidenceReader(
                            accessor, current, platform, db, config);
                        var decision = await new Gate(reader).InspectAsync(
                            new Gate.Intent(Convert.ToString(
                                http.Request.RouteValues["tenantId"]),
                                Rules.Command.Suspend), http.RequestAborted);
                        http.Response.StatusCode =
                            decision.MayProceedToReadOnlyApplicationPreflight
                                ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden;
                        await http.Response.WriteAsync(decision.Code);
                    }).RequireAuthorization());
            })).StartAsync();

    private static Task<HttpResponseMessage> ReadAsync(
        HttpClient client, int tenant, string? jwt) =>
        ReadAsync(client, tenant.ToString(System.Globalization.CultureInfo.InvariantCulture), jwt);

    private static async Task<HttpResponseMessage> ReadAsync(
        HttpClient client, string tenant, string? jwt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "/__c0203_real/" + tenant);
        if (jwt is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", jwt);
        // Spoofing a tenant via headers may not override server route/DB state.
        request.Headers.Add("X-Empresa-Id", "999999");
        request.Headers.Add("X-Rol-Id", "1");
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task RealReaderRequiresSignedJwtLiveGlobalRoleGrantAndActiveTenant()
    {
        var baseConnection = BaseConnection();
        var schema = "c0203_v22_real_" + Guid.NewGuid().ToString("N");
        var scoped = baseConnection.Replace(
            "Database=" + BaseDb + ";", "Database=" + schema + ";",
            StringComparison.Ordinal);
        if (scoped == baseConnection)
            throw new InvalidOperationException("MYSQL84_SCHEMA_REWRITE_REQUIRED");
        await using var admin = new MySqlConnection(baseConnection);
        await admin.OpenAsync();
        await using (var version = admin.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
        }

        try
        {
            await using (var create = admin.CreateCommand())
            {
                create.CommandText = "CREATE DATABASE " + schema;
                await create.ExecuteNonQueryAsync();
            }
            await using var sql = new MySqlConnection(scoped);
            await sql.OpenAsync();
            async Task ExecuteAsync(string statement)
            {
                await using var command = sql.CreateCommand();
                command.CommandText = statement;
                await command.ExecuteNonQueryAsync();
            }

            var ddl = new[]
            {
                "CREATE TABLE Usuarios (Id INT PRIMARY KEY, NombreUsuario VARCHAR(150) NOT NULL, Activo TINYINT NOT NULL, Bloqueado TINYINT NOT NULL, Eliminado TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Roles (Id INT PRIMARY KEY, Nombre VARCHAR(150) NOT NULL, Activo TINYINT NOT NULL, Eliminado TINYINT NOT NULL, EsAdministrador TINYINT NOT NULL, EsSistema TINYINT NOT NULL, Ambito INT NOT NULL, EmpresaId INT NULL) ENGINE=InnoDB",
                "CREATE TABLE UsuarioRolesPlataforma (Id INT PRIMARY KEY, UsuarioId INT NOT NULL, RolId INT NOT NULL, Activa TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Permisos (Id INT PRIMARY KEY, Modulo INT NOT NULL, Accion INT NOT NULL, Ambito INT NOT NULL, Activo TINYINT NOT NULL, Eliminado TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE RolPermisos (Id INT PRIMARY KEY, RolId INT NOT NULL, PermisoId INT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Empresas (Id INT PRIMARY KEY, Activa TINYINT NOT NULL) ENGINE=InnoDB"
            };
            foreach (var statement in ddl)
                await ExecuteAsync(statement);
            await ExecuteAsync("INSERT INTO Usuarios(Id,NombreUsuario,Activo,Bloqueado,Eliminado) VALUES(911,'platform.operator',1,0,0)");
            await ExecuteAsync("INSERT INTO Roles(Id,Nombre,Activo,Eliminado,EsAdministrador,EsSistema,Ambito,EmpresaId) VALUES(17,'PlatformAdmin',1,0,1,1,2,NULL)");
            await ExecuteAsync("INSERT INTO UsuarioRolesPlataforma(Id,UsuarioId,RolId,Activa) VALUES(31,911,17,1)");
            await ExecuteAsync("INSERT INTO Empresas(Id,Activa) VALUES(1,1),(811,1),(812,1)");
            await using (var permission = sql.CreateCommand())
            {
                permission.CommandText = "INSERT INTO Permisos(Id,Modulo,Accion,Ambito,Activo,Eliminado) VALUES(51,@module,@action,2,1,0)";
                permission.Parameters.AddWithValue("@module", (int)ModuloSistema.Configuracion);
                permission.Parameters.AddWithValue("@action", (int)AccionPermiso.CambiarEstado);
                await permission.ExecuteNonQueryAsync();
            }
            await ExecuteAsync("INSERT INTO RolPermisos(Id,RolId,PermisoId) VALUES(61,17,51)");

            using var host = await StartAsync(scoped);
            using var client = host.GetTestClient();
            var jwt = Token();
            using (var noToken = await ReadAsync(client, 811, null))
                Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
            using (var forged = await ReadAsync(client, 811, Token(forged: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var forgedSubject = await ReadAsync(
                       client, 811, Token(wrongSubject: true)))
                Assert.Equal(HttpStatusCode.Forbidden, forgedSubject.StatusCode);
            using (var positiveA = await ReadAsync(client, 811, jwt))
            {
                Assert.Equal(HttpStatusCode.OK, positiveA.StatusCode);
                Assert.Equal("VERIFIED_READONLY_PREFLIGHT_ONLY",
                    await positiveA.Content.ReadAsStringAsync());
            }
            // A live platform grant is global, but the target tenant must exist.
            using (var positiveB = await ReadAsync(client, 812, jwt))
                Assert.Equal(HttpStatusCode.OK, positiveB.StatusCode);
            using (var positiveSingleDigit = await ReadAsync(client, 1, jwt))
                Assert.Equal(HttpStatusCode.OK, positiveSingleDigit.StatusCode);

            // SQL numeric aliases must not bypass tenant-scoped receipts/audit.
            // ASP.NET int route constraints can accept leading-zero segments;
            // the same tenant must have exactly one canonical decimal spelling.
            foreach (var alias in new[] { "0811", "000811", "0812", "01" })
            {
                using var deniedAlias = await ReadAsync(client, alias, jwt);
                Assert.True(deniedAlias.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
                    $"Noncanonical tenant {alias} unexpectedly returned {deniedAlias.StatusCode}");
                if (deniedAlias.StatusCode == HttpStatusCode.Forbidden)
                    Assert.Equal("LIFECYCLE_ACCESS_DENIED",
                        await deniedAlias.Content.ReadAsStringAsync());
            }

            // New V22 adversarial boundary: a second active platform grant for
            // the same signed-JWT actor is ambiguous, even when both point to
            // an otherwise valid role. Never silently select either grant.
            // Use committed MySQL writes so the HTTP reader's own DbContext
            // can observe the real database state across requests.
            await ExecuteAsync("INSERT INTO UsuarioRolesPlataforma(Id,UsuarioId,RolId,Activa) VALUES(32,911,17,1)");
            using (var ambiguousA = await ReadAsync(client, 811, jwt))
            {
                Assert.Equal(HttpStatusCode.Forbidden, ambiguousA.StatusCode);
                Assert.Equal("LIFECYCLE_ACCESS_DENIED",
                    await ambiguousA.Content.ReadAsStringAsync());
            }
            using (var ambiguousB = await ReadAsync(client, 812, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, ambiguousB.StatusCode);
            await ExecuteAsync("DELETE FROM UsuarioRolesPlataforma WHERE Id=32");
            using (var recoveredAfterRemovingDuplicate = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.OK, recoveredAfterRemovingDuplicate.StatusCode);

            using (var missingTenant = await ReadAsync(client, 813, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, missingTenant.StatusCode);
            await ExecuteAsync("UPDATE Empresas SET Activa=0 WHERE Id=812");
            using (var inactiveB = await ReadAsync(client, 812, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, inactiveB.StatusCode);

            await ExecuteAsync("DELETE FROM RolPermisos WHERE Id=61");
            using (var grantRevoked = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, grantRevoked.StatusCode);
            await using (var transaction = await sql.BeginTransactionAsync())
            {
                await using var reinstatement = sql.CreateCommand();
                reinstatement.Transaction = transaction;
                reinstatement.CommandText =
                    "INSERT INTO RolPermisos(Id,RolId,PermisoId) VALUES(61,17,51)";
                await reinstatement.ExecuteNonQueryAsync();
                await transaction.RollbackAsync();
            }
            using (var rolledBack = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, rolledBack.StatusCode);

            await ExecuteAsync("INSERT INTO RolPermisos(Id,RolId,PermisoId) VALUES(61,17,51)");
            await ExecuteAsync("UPDATE UsuarioRolesPlataforma SET Activa=0 WHERE Id=31");
            using (var roleRevoked = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, roleRevoked.StatusCode);
            await ExecuteAsync("UPDATE UsuarioRolesPlataforma SET Activa=1 WHERE Id=31");
            await ExecuteAsync("UPDATE Usuarios SET Bloqueado=1 WHERE Id=911");
            using (var blocked = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        }
        finally
        {
            await using var drop = admin.CreateCommand();
            drop.CommandText = "DROP DATABASE IF EXISTS " + schema;
            await drop.ExecuteNonQueryAsync();
        }
    }
}
