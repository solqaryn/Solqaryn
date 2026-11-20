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
using Moq;
using MySql.EntityFrameworkCore.Extensions;
using MySqlConnector;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C11-03.M03.03 R2: real JWT Bearer + HTTP + the actual C1103 API reader,
/// actual UsuarioScopeService/PermisoService/RolPermisoRepository and Oracle EF
/// against a unique, disposable MySQL 8.4 database. Test-only endpoint;
/// no outbox writes, relay, production hosts or production credentials.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0303RealMysqlEfJwtHttpIntegrationTests
{
    private const string ExpectedDatabase = "solqaryn_c1502_m0503";
    private const string Issuer = "solqaryn-c1103-real-ef-test";
    private const string Audience = "solqaryn-c1103-real-ef-client";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c1103-v22-real-ef-mysql84-test-only-valid-signing-key"));
    private static readonly SymmetricSecurityKey ForgedKey = new(
        Encoding.UTF8.GetBytes("c1103-v22-real-ef-mysql84-test-only-forged-sign-key"));

    private static string BaseConnection()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("MYSQL84_EPHEMERAL_CONNECTION_REQUIRED");
        var info = new MySqlConnectionStringBuilder(raw);
        if (info.Server != "127.0.0.1" || info.Port != 3306 ||
            info.Database != ExpectedDatabase || info.UserID != "root" ||
            info.SslMode != MySqlSslMode.Disabled)
            throw new InvalidOperationException("MYSQL84_EPHEMERAL_LOOPBACK_ONLY");
        return raw;
    }

    private static string Token(int actor = 911, bool forged = false, bool mismatched = false)
    {
        var name = "user.c1103";
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub,
                    mismatched ? "some-other-user" : name),
                new Claim("nombreUsuario", name),
                new Claim("id", actor.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                forged ? ForgedKey : Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static Task<IHost> StartAsync(string scopedConnection) =>
        new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddHttpContextAccessor();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        // Production default maps sub to NameIdentifier, never numeric ID.
                        options.MapInboundClaims = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = Issuer,
                            ValidateAudience = true,
                            ValidAudience = Audience,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = Key,
                            RequireSignedTokens = true,
                            RequireExpirationTime = true
                        };
                    });
                services.AddAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints
                    .MapGet("/__c1103_test/real-ef/{tenantId:int}", async http =>
                    {
                        var tenant = Convert.ToInt32(http.Request.RouteValues["tenantId"],
                            System.Globalization.CultureInfo.InvariantCulture);
                        var accessor = http.RequestServices
                            .GetRequiredService<IHttpContextAccessor>();
                        var current = new CurrentUserService(accessor);
                        var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseMySQL(scopedConnection).Options;
                        await using var db = new AppDbContext(options,
                            tenantHttpContextAccessor: accessor);
                        var scope = new UsuarioScopeService(db, current);
                        var permission = new PermisoService(
                            new RolPermisoRepository(db),
                            Mock.Of<IRolRepository>(), Mock.Of<IPermisoRepository>(),
                            Mock.Of<IAuditoriaService>(), current, scope);
                        var settings = new ConfigurationBuilder().AddInMemoryCollection(
                            new Dictionary<string, string?>
                            {
                                ["OutboxPlanning:EnableReadOnlyAuthorizationProbe"] = "true"
                            }).Build();
                        var reader = new C1103OutboxVerifiedMembershipReader(
                            accessor, current, scope, permission, settings);
                        var verified = await reader.ReadCurrentAsync(
                            911, tenant, http.RequestAborted);
                        http.Response.StatusCode = verified is null
                            ? StatusCodes.Status403Forbidden : StatusCodes.Status200OK;
                    }).RequireAuthorization());
            })).StartAsync();

    private static async Task<HttpResponseMessage> ReadAsync(
        HttpClient client, int tenant, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "/__c1103_test/real-ef/" + tenant);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Empresa-Id", "999999");
        request.Headers.Add("X-Rol-Id", "1");
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task RealOracleEfJwtHttpTenantGrantRevocationAndRollbackFailClosed()
    {
        var original = BaseConnection();
        var schema = "c1103_v22_auth_" + Guid.NewGuid().ToString("N");
        // MySqlConnector normalizes SslMode=Disabled into None; Oracle EF rejects
        // that normalized spelling. Preserve the original loopback connection.
        var scoped = original.Replace("Database=" + ExpectedDatabase + ";",
            "Database=" + schema + ";", StringComparison.Ordinal);
        if (scoped == original)
            throw new InvalidOperationException("MYSQL84_EPHEMERAL_SCHEMA_REWRITE_REQUIRED");

        await using var admin = new MySqlConnection(original);
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
            string[] tables =
            [
                "CREATE TABLE Empresas (Id INT PRIMARY KEY, Nombre VARCHAR(200) NOT NULL, Activa TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Roles (Id INT PRIMARY KEY, Nombre VARCHAR(80) NOT NULL, Activo TINYINT NOT NULL, Eliminado TINYINT NOT NULL, Ambito INT NOT NULL, EmpresaId INT NULL, EsSistema TINYINT NOT NULL, EsAdministrador TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Usuarios (Id INT PRIMARY KEY, RolId INT NOT NULL, Activo TINYINT NOT NULL, Bloqueado TINYINT NOT NULL, Eliminado TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE UsuarioEmpresas (Id INT PRIMARY KEY, UsuarioId INT NOT NULL, EmpresaId INT NOT NULL, RolId INT NOT NULL, Activa TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Permisos (Id INT PRIMARY KEY, Modulo INT NOT NULL, Accion INT NOT NULL, Activo TINYINT NOT NULL, Eliminado TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE RolPermisos (Id INT PRIMARY KEY, RolId INT NOT NULL, PermisoId INT NOT NULL, EmpresaId INT NULL) ENGINE=InnoDB"
            ];
            foreach (var ddl in tables)
            {
                await using var cmd = sql.CreateCommand();
                cmd.CommandText = ddl;
                await cmd.ExecuteNonQueryAsync();
            }

            async Task SqlAsync(string statement)
            {
                await using var cmd = sql.CreateCommand();
                cmd.CommandText = statement;
                await cmd.ExecuteNonQueryAsync();
            }

            await SqlAsync("INSERT INTO Empresas(Id,Nombre,Activa) VALUES(811,'Tenant A',1),(812,'Tenant B',1)");
            await SqlAsync("INSERT INTO Roles(Id,Nombre,Activo,Eliminado,Ambito,EmpresaId,EsSistema,EsAdministrador) VALUES(17,'A role',1,0,1,811,0,0),(18,'B role',1,0,1,812,0,0)");
            await SqlAsync("INSERT INTO Usuarios(Id,RolId,Activo,Bloqueado,Eliminado) VALUES(911,18,1,0,0)");
            await SqlAsync("INSERT INTO UsuarioEmpresas(Id,UsuarioId,EmpresaId,RolId,Activa) VALUES(931,911,811,17,1),(932,911,812,18,1)");
            await using (var permission = sql.CreateCommand())
            {
                permission.CommandText = "INSERT INTO Permisos(Id,Modulo,Accion,Activo,Eliminado) VALUES(951,@module,@action,1,0)";
                permission.Parameters.AddWithValue("@module", (int)ModuloSistema.Configuracion);
                permission.Parameters.AddWithValue("@action", (int)AccionPermiso.Crear);
                await permission.ExecuteNonQueryAsync();
            }
            await SqlAsync("INSERT INTO RolPermisos(Id,RolId,PermisoId,EmpresaId) VALUES(991,17,951,811)");

            // Prove actual membership and permission services before HTTP reader.
            await using (var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>().UseMySQL(scoped).Options))
            {
                var actor = new Mock<ICurrentUserService>();
                actor.SetupGet(x => x.EstaAutenticado).Returns(true);
                actor.SetupGet(x => x.UsuarioId).Returns((int?)911);
                var scope = new UsuarioScopeService(db, actor.Object);
                var actual = await scope.ObtenerActualAsync(811);
                Assert.NotNull(actual);
                Assert.Equal(17, actual.RolId);
                var grants = new PermisoService(new RolPermisoRepository(db),
                    Mock.Of<IRolRepository>(), Mock.Of<IPermisoRepository>(),
                    Mock.Of<IAuditoriaService>(), actor.Object, scope);
                Assert.True(await grants.TienePermisoAsync(
                    811, ModuloSistema.Configuracion, AccionPermiso.Crear));
                Assert.False(await grants.TienePermisoAsync(
                    812, ModuloSistema.Configuracion, AccionPermiso.Crear));
                Assert.Equal(811, await db.RolPermisos
                    .Where(g => g.RolId == 17)
                    .Select(g => EF.Property<int?>(g, "EmpresaId"))
                    .SingleAsync());
            }

            using var host = await StartAsync(scoped);
            using var client = host.GetTestClient();
            var jwt = Token();
            using (var allowed = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            using (var foreignTenant = await ReadAsync(client, 812, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, foreignTenant.StatusCode);
            using (var forged = await ReadAsync(client, 811, Token(forged: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var anonymous = await ReadAsync(client, 811, null))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            using (var mismatched = await ReadAsync(client, 811, Token(mismatched: true)))
                Assert.Equal(HttpStatusCode.Forbidden, mismatched.StatusCode);
            using (var wrongActor = await ReadAsync(client, 811, Token(actor: 912)))
                Assert.Equal(HttpStatusCode.Forbidden, wrongActor.StatusCode);

            await SqlAsync("DELETE FROM RolPermisos WHERE Id=991");
            using (var revoked = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
            await using (var tx = await sql.BeginTransactionAsync())
            {
                await using var phantom = sql.CreateCommand();
                phantom.Transaction = tx;
                phantom.CommandText = "INSERT INTO RolPermisos(Id,RolId,PermisoId,EmpresaId) VALUES(991,17,951,811)";
                await phantom.ExecuteNonQueryAsync();
                await tx.RollbackAsync();
            }
            using (var rolledBackGrant = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, rolledBackGrant.StatusCode);
            await SqlAsync("INSERT INTO RolPermisos(Id,RolId,PermisoId,EmpresaId) VALUES(991,17,951,811)");
            await SqlAsync("UPDATE UsuarioEmpresas SET RolId=18 WHERE Id=931");
            using (var wrongTenantRole = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, wrongTenantRole.StatusCode);
            await SqlAsync("UPDATE UsuarioEmpresas SET RolId=17,Activa=0 WHERE Id=931");
            using (var revokedMembership = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, revokedMembership.StatusCode);
            await SqlAsync("UPDATE UsuarioEmpresas SET Activa=1 WHERE Id=931");
            await SqlAsync("UPDATE Usuarios SET Bloqueado=1 WHERE Id=911");
            using (var blockedUser = await ReadAsync(client, 811, jwt))
                Assert.Equal(HttpStatusCode.Forbidden, blockedUser.StatusCode);
        }
        finally
        {
            await using var drop = admin.CreateCommand();
            drop.CommandText = "DROP DATABASE IF EXISTS " + schema;
            await drop.ExecuteNonQueryAsync();
        }
    }
}
