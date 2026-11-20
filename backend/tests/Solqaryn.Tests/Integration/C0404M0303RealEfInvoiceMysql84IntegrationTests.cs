using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
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
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Application.Services;
using Solqaryn.Infrastructure.Services;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// DEV04 V22: real signed JWT + HTTP + the actual API reader and Oracle EF
/// against ephemeral MySQL 8.4 Facturas. Scope/grant collaborators remain
/// intentionally stubbed; this does NOT certify real UsuarioEmpresa/RolPermiso.
/// No provider calls, financial writes, production DB or production API.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0404M0303RealEfInvoiceMysql84IntegrationTests
{
    private const string ExpectedDatabase = "solqaryn_c1502_m0503";
    private const string Issuer = "solqaryn-c0404-realef-test";
    private const string Audience = "solqaryn-c0404-realef-client";
    private static readonly SymmetricSecurityKey SigningKey = new(
        Encoding.UTF8.GetBytes("c0404-v22-realef-disposable-valid-signing-key-2026"));
    private static readonly SymmetricSecurityKey ForgedKey = new(
        Encoding.UTF8.GetBytes("c0404-v22-realef-disposable-forged-signing-key-2026"));

    private static string EphemeralConnection()
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

    private static async Task PrepareAsync()
    {
        await using var connection = new MySqlConnection(EphemeralConnection());
        await connection.OpenAsync();
        await using var version = connection.CreateCommand();
        version.CommandText = "SELECT VERSION()";
        Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
        await using var table = connection.CreateCommand();
        table.CommandText = """
            CREATE TABLE IF NOT EXISTS Facturas(
                Id INT NOT NULL PRIMARY KEY,
                EmpresaId INT NULL
            ) ENGINE=InnoDB
            """;
        await table.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(string sql, int invoice, int tenant)
    {
        await using var connection = new MySqlConnection(EphemeralConnection());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", invoice);
        command.Parameters.AddWithValue("@tenant", tenant);
        await command.ExecuteNonQueryAsync();
    }

    private static string Jwt(int actor, bool forged = false)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: new[] { new Claim("id", actor.ToString()) },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                forged ? ForgedKey : SigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static Task<IHost> StartAsync(Func<bool> grantStillActive) =>
        new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddHttpContextAccessor();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true, ValidIssuer = Issuer,
                            ValidateAudience = true, ValidAudience = Audience,
                            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = SigningKey,
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
                app.UseEndpoints(endpoints =>
                    endpoints.MapGet("/__v22_test/realef-payment/{tenantId:int}/{invoiceId:int}",
                        async http =>
                        {
                            var tenant = int.Parse(Convert.ToString(http.Request.RouteValues["tenantId"])!);
                            var invoice = int.Parse(Convert.ToString(http.Request.RouteValues["invoiceId"])!);
                            int.TryParse(http.User.FindFirst("id")?.Value, out var actor);

                            var current = new Mock<ICurrentUserService>();
                            current.SetupGet(x => x.EstaAutenticado).Returns(actor > 0);
                            current.SetupGet(x => x.UsuarioId).Returns((int?)actor);
                            var scope = new Mock<IUsuarioScopeService>();
                            scope.Setup(x => x.ObtenerActualAsync(
                                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .Returns((int target, CancellationToken _) =>
                                    Task.FromResult<UsuarioTenantScopeActual?>(
                                        target == 401 && actor == 701
                                            ? new UsuarioTenantScopeActual(actor, 401, 801, "TestRole", false)
                                            : null));
                            var permission = new Mock<IPermisoService>();
                            permission.Setup(x => x.TienePermisoAsync(
                                    It.IsAny<int>(), ModuloSistema.Facturacion, AccionPermiso.Compartir))
                                .Returns((int target, ModuloSistema _, AccionPermiso __) =>
                                    Task.FromResult(target == 401 && grantStillActive()));

                            var accessor = http.RequestServices.GetRequiredService<IHttpContextAccessor>();
                            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseMySQL(EphemeralConnection()).Options;
                            await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);
                            var config = new ConfigurationBuilder().AddInMemoryCollection(
                                new Dictionary<string, string?>
                                {
                                    ["PaymentPlanning:EnableReadOnlyAuthorizationProbe"] = "true"
                                }).Build();
                            var reader = new C0404PaymentVerifiedEvidenceReader(
                                accessor, current.Object, scope.Object, permission.Object, db, config);
                            var decision = await new C0404PaymentAuthorizationGate(reader).InspectAsync(
                                new C0404PaymentAuthorizationGate.Request(
                                    tenant, invoice, C0404PaymentAuthorizationGate.Operation.CreateLink),
                                http.RequestAborted);
                            http.Response.StatusCode = decision.MayInspectPureApplicationPlan
                                ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden;
                            await http.Response.WriteAsync(decision.Code);
                        }).RequireAuthorization());
            })).StartAsync();

    private static async Task<HttpResponseMessage> GetAsync(
        HttpClient client, int tenant, int invoice, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/__v22_test/realef-payment/{tenant}/{invoice}");
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Empresa-Id", "999999");
        request.Headers.Add("X-Rol-Id", "1");
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task RealReaderOracleEfMysql84ChecksInvoiceOwnershipAndFailClosedJwt()
    {
        await PrepareAsync();
        var invoiceA = RandomNumberGenerator.GetInt32(1_000_000, int.MaxValue - 10);
        var invoiceB = invoiceA + 1;
        var rolledBack = invoiceA + 2;
        const string insert = "INSERT INTO Facturas(Id,EmpresaId) VALUES(@id,@tenant)";
        const string delete = "DELETE FROM Facturas WHERE Id=@id";
        try
        {
            await ExecuteAsync(insert, invoiceA, 401);
            await ExecuteAsync(insert, invoiceB, 402);
            var grant = true;
            using var host = await StartAsync(() => grant);
            using var client = host.GetTestClient();
            var jwtA = Jwt(701);

            using (var missingJwt = await GetAsync(client, 401, invoiceA, null))
                Assert.Equal(HttpStatusCode.Unauthorized, missingJwt.StatusCode);
            using (var forged = await GetAsync(client, 401, invoiceA, Jwt(701, forged: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var allowed = await GetAsync(client, 401, invoiceA, jwtA))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Equal("READONLY_PAYMENT_PLAN_PREFLIGHT_ONLY",
                    await allowed.Content.ReadAsStringAsync());
            }
            using (var foreignInvoice = await GetAsync(client, 401, invoiceB, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, foreignInvoice.StatusCode);
            using (var wrongTenant = await GetAsync(client, 402, invoiceA, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, wrongTenant.StatusCode);
            using (var wrongActor = await GetAsync(client, 401, invoiceA, Jwt(702)))
                Assert.Equal(HttpStatusCode.Forbidden, wrongActor.StatusCode);

            grant = false; // Mocked grant, NOT a real RolPermiso revocation.
            using (var revokedMockGrant = await GetAsync(client, 401, invoiceA, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, revokedMockGrant.StatusCode);
            grant = true;
            await ExecuteAsync("UPDATE Facturas SET EmpresaId=@tenant WHERE Id=@id", invoiceA, 402);
            using (var ownershipMoved = await GetAsync(client, 401, invoiceA, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, ownershipMoved.StatusCode);

            await using (var db = new MySqlConnection(EphemeralConnection()))
            {
                await db.OpenAsync();
                await using var transaction = await db.BeginTransactionAsync();
                await using var command = db.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = insert;
                command.Parameters.AddWithValue("@id", rolledBack);
                command.Parameters.AddWithValue("@tenant", 401);
                await command.ExecuteNonQueryAsync();
                await transaction.RollbackAsync();
            }
            using (var noPhantomInvoice = await GetAsync(client, 401, rolledBack, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, noPhantomInvoice.StatusCode);
        }
        finally
        {
            await ExecuteAsync(delete, invoiceA, 0);
            await ExecuteAsync(delete, invoiceB, 0);
            await ExecuteAsync(delete, rolledBack, 0);
        }
    }

    // V22: the real membership/RolPermiso services run their SQL against a disposable
    // loopback-only MySQL schema. This is more than a synthetic role/permission mock,
    // but the HTTP route remains an isolated TestServer probe, not a live PSP.
    private static Task<IHost> StartRealAuthorityAsync(string connectionString) =>
        new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddHttpContextAccessor();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true, ValidIssuer = Issuer,
                            ValidateAudience = true, ValidAudience = Audience,
                            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true, IssuerSigningKey = SigningKey,
                            RequireSignedTokens = true, RequireExpirationTime = true
                        };
                    });
                services.AddAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                    endpoints.MapGet("/__v22_test/realef-payment/{tenantId:int}/{invoiceId:int}",
                        async http =>
                        {
                            var tenant = int.Parse(Convert.ToString(http.Request.RouteValues["tenantId"])!);
                            var invoice = int.Parse(Convert.ToString(http.Request.RouteValues["invoiceId"])!);
                            var accessor = http.RequestServices.GetRequiredService<IHttpContextAccessor>();
                            var options = new DbContextOptionsBuilder<AppDbContext>()
                                .UseMySQL(connectionString).Options;
                            await using var db = new AppDbContext(options,
                                tenantHttpContextAccessor: accessor);
                            var current = new CurrentUserService(accessor);
                            var scope = new UsuarioScopeService(db, current);
                            var permission = new PermisoService(
                                new RolPermisoRepository(db),
                                Mock.Of<IRolRepository>(),
                                Mock.Of<IPermisoRepository>(),
                                Mock.Of<IAuditoriaService>(),
                                current, scope);
                            var config = new ConfigurationBuilder().AddInMemoryCollection(
                                new Dictionary<string, string?>
                                {
                                    ["PaymentPlanning:EnableReadOnlyAuthorizationProbe"] = "true"
                                }).Build();
                            var reader = new C0404PaymentVerifiedEvidenceReader(
                                accessor, current, scope, permission, db, config);
                            var decision = await new C0404PaymentAuthorizationGate(reader).InspectAsync(
                                new C0404PaymentAuthorizationGate.Request(
                                    tenant, invoice, C0404PaymentAuthorizationGate.Operation.CreateLink),
                                http.RequestAborted);
                            http.Response.StatusCode = decision.MayInspectPureApplicationPlan
                                ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden;
                            await http.Response.WriteAsync(decision.Code);
                        }).RequireAuthorization());
            })).StartAsync();

    [Fact]
    public async Task RealUsuarioEmpresaRolPermisoRevocationJwtHttpMysql84()
    {
        // Fail closed if the environment points outside the isolated GitHub-hosted
        // MySQL container. A separate GUID schema prevents parallel-test collisions.
        var original = EphemeralConnection();
        var schema = "c0404_v22_auth_" + Guid.NewGuid().ToString("N");
        // Preserve the Oracle provider's required SslMode=Disabled spelling.
        // MySqlConnector normalizes it to None when it rebuilds the string;
        // Oracle's MySql.Data parser rejects that value (CI 44720913552).
        var scoped = original.Replace("Database=" + ExpectedDatabase + ";",
            "Database=" + schema + ";", StringComparison.Ordinal);
        if (scoped == original)
            throw new InvalidOperationException("MYSQL84_EPHEMERAL_DATABASE_REWRITE_REQUIRED");
        await using var admin = new MySqlConnection(original);
        await admin.OpenAsync();
        try
        {
            await using (var create = admin.CreateCommand())
            {
                create.CommandText = "CREATE DATABASE " + schema;
                await create.ExecuteNonQueryAsync();
            }

            await using var sql = new MySqlConnection(scoped);
            await sql.OpenAsync();
            string[] ddl =
            {
                "CREATE TABLE Empresas (Id INT PRIMARY KEY, Nombre VARCHAR(200) NOT NULL, Activa TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Roles (Id INT PRIMARY KEY, Nombre VARCHAR(80) NOT NULL, Activo TINYINT NOT NULL, Eliminado TINYINT NOT NULL, Ambito INT NOT NULL, EmpresaId INT NULL, EsSistema TINYINT NOT NULL, EsAdministrador TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Usuarios (Id INT PRIMARY KEY, RolId INT NOT NULL, Activo TINYINT NOT NULL, Bloqueado TINYINT NOT NULL, Eliminado TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE UsuarioEmpresas (Id INT PRIMARY KEY, UsuarioId INT NOT NULL, EmpresaId INT NOT NULL, RolId INT NOT NULL, Activa TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Permisos (Id INT PRIMARY KEY, Modulo INT NOT NULL, Accion INT NOT NULL, Activo TINYINT NOT NULL, Eliminado TINYINT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE RolPermisos (Id INT PRIMARY KEY, RolId INT NOT NULL, PermisoId INT NOT NULL) ENGINE=InnoDB",
                "CREATE TABLE Facturas (Id INT PRIMARY KEY, EmpresaId INT NULL) ENGINE=InnoDB"
            };
            foreach (var ddlSql in ddl)
            {
                await using var cmd = sql.CreateCommand();
                cmd.CommandText = ddlSql;
                await cmd.ExecuteNonQueryAsync();
            }

            string[] seed =
            {
                "INSERT INTO Empresas(Id,Nombre,Activa) VALUES(401,'Empresa A',1),(402,'Empresa B',1)",
                "INSERT INTO Roles(Id,Nombre,Activo,Eliminado,Ambito,EmpresaId,EsSistema,EsAdministrador) VALUES(801,'Operador A',1,0,1,401,0,0),(802,'Operador B',1,0,1,402,0,0)",
                "INSERT INTO Usuarios(Id,RolId,Activo,Bloqueado,Eliminado) VALUES(701,802,1,0,0)",
                "INSERT INTO UsuarioEmpresas(Id,UsuarioId,EmpresaId,RolId,Activa) VALUES(901,701,401,801,1),(902,701,402,802,1)",
                "INSERT INTO Facturas(Id,EmpresaId) VALUES(5001,401),(5002,402)"
            };
            foreach (var seedSql in seed)
            {
                await using var cmd = sql.CreateCommand();
                cmd.CommandText = seedSql;
                await cmd.ExecuteNonQueryAsync();
            }
            await using (var insertPermission = sql.CreateCommand())
            {
                insertPermission.CommandText =
                    "INSERT INTO Permisos(Id,Modulo,Accion,Activo,Eliminado) VALUES(951,@module,@action,1,0)";
                insertPermission.Parameters.AddWithValue("@module",
                    (int)ModuloSistema.Facturacion);
                insertPermission.Parameters.AddWithValue("@action",
                    (int)AccionPermiso.Compartir);
                await insertPermission.ExecuteNonQueryAsync();
            }
            async Task ChangeAsync(string statement)
            {
                await using var cmd = sql.CreateCommand();
                cmd.CommandText = statement;
                await cmd.ExecuteNonQueryAsync();
            }
            await ChangeAsync("INSERT INTO RolPermisos(Id,RolId,PermisoId) VALUES(991,801,951)");

            using var host = await StartRealAuthorityAsync(scoped);
            using var client = host.GetTestClient();
            var validA = Jwt(701);
            using (var success = await GetAsync(client, 401, 5001, validA))
            {
                Assert.Equal(HttpStatusCode.OK, success.StatusCode);
                Assert.Equal("READONLY_PAYMENT_PLAN_PREFLIGHT_ONLY",
                    await success.Content.ReadAsStringAsync());
            }
            using (var forged = await GetAsync(client, 401, 5001, Jwt(701, true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var missing = await GetAsync(client, 401, 5001, null))
                Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
            using (var acrossTenant = await GetAsync(client, 402, 5002, validA))
                Assert.Equal(HttpStatusCode.Forbidden, acrossTenant.StatusCode);
            using (var foreignInvoice = await GetAsync(client, 401, 5002, validA))
                Assert.Equal(HttpStatusCode.Forbidden, foreignInvoice.StatusCode);

            await ChangeAsync("DELETE FROM RolPermisos WHERE Id=991");
            using (var revokedGrant = await GetAsync(client, 401, 5001, validA))
                Assert.Equal(HttpStatusCode.Forbidden, revokedGrant.StatusCode);
            await ChangeAsync("INSERT INTO RolPermisos(Id,RolId,PermisoId) VALUES(991,801,951)");
            await ChangeAsync("UPDATE UsuarioEmpresas SET RolId=802 WHERE Id=901");
            using (var roleBInA = await GetAsync(client, 401, 5001, validA))
                Assert.Equal(HttpStatusCode.Forbidden, roleBInA.StatusCode);
            await ChangeAsync("UPDATE UsuarioEmpresas SET RolId=801,Activa=0 WHERE Id=901");
            using (var revokedMembership = await GetAsync(client, 401, 5001, validA))
                Assert.Equal(HttpStatusCode.Forbidden, revokedMembership.StatusCode);
            await ChangeAsync("UPDATE UsuarioEmpresas SET Activa=1 WHERE Id=901");
            await ChangeAsync("UPDATE Usuarios SET Bloqueado=1 WHERE Id=701");
            using (var blockedUser = await GetAsync(client, 401, 5001, validA))
                Assert.Equal(HttpStatusCode.Forbidden, blockedUser.StatusCode);
        }
        finally
        {
            // Only the newly created GUID-scoped schema is removed; never touch
            // Aiven, any shared database, credentials, or financial records.
            await using var drop = admin.CreateCommand();
            drop.CommandText = "DROP DATABASE IF EXISTS " + schema;
            await drop.ExecuteNonQueryAsync();
        }
    }
}
