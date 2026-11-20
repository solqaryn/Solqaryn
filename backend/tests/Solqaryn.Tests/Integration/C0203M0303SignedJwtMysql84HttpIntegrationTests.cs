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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.C0203TenantLifecycleAuthorizationGate;
using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// Signed JWT, actual HTTP TestServer, ephemeral MySQL 8.4 and actual
/// C0203 Application gate. The SQL fixture does not represent the real
/// UsuarioEmpresa/RolPermiso adapter; production acceptance remains pending.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0203M0303SignedJwtMysql84HttpIntegrationTests
{
    private const string DbName = "solqaryn_c1502_m0503";
    private const string Issuer = "solqaryn-c0203-v22-test";
    private const string Audience = "solqaryn-c0203-client";
    private static readonly SymmetricSecurityKey GoodKey = new(
        Encoding.UTF8.GetBytes("c0203-v22-valid-ephemeral-jwt-key-only-for-tests"));
    private static readonly SymmetricSecurityKey WrongKey = new(
        Encoding.UTF8.GetBytes("c0203-v22-forged-ephemeral-jwt-key-only-tests"));

    private static string Connection()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("MYSQL84_EPHEMERAL_REQUIRED");
        var c = new MySqlConnectionStringBuilder(raw);
        if (c.Server != "127.0.0.1" || c.Port != 3306 || c.Database != DbName ||
            c.UserID != "root" || c.SslMode != MySqlSslMode.Disabled)
            throw new InvalidOperationException("MYSQL84_LOOPBACK_ONLY");
        return raw;
    }

    private static async Task SqlAsync(string sql, int tenant, int actor)
    {
        await using var db = new MySqlConnection(Connection());
        await db.OpenAsync();
        await using var command = db.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@t", tenant);
        command.Parameters.AddWithValue("@a", actor);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class Reader(int actor) : IC0203VerifiedOperatorEvidenceReader
    {
        public async ValueTask<Gate.TrustedOperatorSnapshot?> ReadAsync(
            string tenantText, Rules.Command operation, CancellationToken ct)
        {
            if (actor <= 0 || !int.TryParse(tenantText, out var tenant))
                return null;
            await using var db = new MySqlConnection(Connection());
            await db.OpenAsync(ct);
            await using var command = db.CreateCommand();
            command.CommandText = """
                SELECT CommandId,MemberActive,GrantActive,Revoked,TenantOwned
                FROM V22C0203AuthFixture
                WHERE TenantId=@t AND ActorId=@a
                """;
            command.Parameters.AddWithValue("@t", tenant);
            command.Parameters.AddWithValue("@a", actor);
            await using var data = await command.ExecuteReaderAsync(ct);
            if (!await data.ReadAsync(ct)) return null;
            bool Flag(int i) => Convert.ToBoolean(data.GetValue(i));
            var verified = (Rules.Command)data.GetInt32(0);
            return new Gate.TrustedOperatorSnapshot
            {
                VerifiedActorId = actor.ToString(), VerifiedTenantId = tenantText,
                VerifiedCommand = verified, ServerControlledEvidence = true,
                BackendJwtSignatureVerified = true, CurrentPlatformGrantVerified = Flag(2),
                CurrentOperatorRoleVerified = Flag(2),
                MembershipAndSessionFresh = Flag(1),
                ActorOrGrantRevoked = Flag(3), TargetOwnershipVerifiedServerSide = Flag(4),
                PolicyRevisionFresh = true, LifecycleFeatureEnabled = true,
                CommandEnabledByTrustedConfig = verified == operation,
                TenantFencedEvidenceRead = true
            };
        }
    }

    private static Task<IHost> StartAsync() =>
        new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true, ValidIssuer = Issuer,
                            ValidateAudience = true, ValidAudience = Audience,
                            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                            ValidateIssuerSigningKey = true, IssuerSigningKey = GoodKey,
                            RequireSignedTokens = true, RequireExpirationTime = true
                        };
                    });
                services.AddAuthorization();
            }).Configure(app =>
            {
                app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapGet(
                    "/__v22_test/lifecycle/{tenant:int}/{op:int}", async http =>
                    {
                        var tenant = Convert.ToString(http.Request.RouteValues["tenant"]);
                        var op = Convert.ToInt32(http.Request.RouteValues["op"]);
                        int.TryParse(http.User.FindFirst("sub")?.Value, out var actor);
                        var result = await new Gate(new Reader(actor)).InspectAsync(
                            new Gate.Intent(tenant, (Rules.Command)op), http.RequestAborted);
                        http.Response.StatusCode = result.MayProceedToReadOnlyApplicationPreflight
                            ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden;
                        await http.Response.WriteAsync(result.Code);
                    }).RequireAuthorization());
            })).StartAsync();

    private static string Token(int actor, bool forged = false) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, actor.ToString()) },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                forged ? WrongKey : GoodKey, SecurityAlgorithms.HmacSha256)));

    private static async Task<HttpResponseMessage> GetAsync(
        HttpClient client, int tenant, Rules.Command op, string? token)
    {
        using var r = new HttpRequestMessage(HttpMethod.Get,
            $"/__v22_test/lifecycle/{tenant}/{(int)op}");
        if (token != null)
            r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        r.Headers.Add("X-Empresa-Id", "999999");
        return await client.SendAsync(r);
    }

    private const string Insert = """
        INSERT INTO V22C0203AuthFixture
        (TenantId,ActorId,CommandId,MemberActive,GrantActive,Revoked,TenantOwned)
        VALUES(@t,@a,3,1,1,0,1)
        """;
    private const string Delete = """
        DELETE FROM V22C0203AuthFixture WHERE TenantId=@t AND ActorId=@a
        """;

    [Fact]
    public async Task RealJwtHttpMysql84DeniesTenantBRevocationForgedJwtCommandReplayAndRollback()
    {
        await using (var db = new MySqlConnection(Connection()))
        {
            await db.OpenAsync();
            await using var version = db.CreateCommand();
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
            await using var table = db.CreateCommand();
            table.CommandText = """
                CREATE TABLE IF NOT EXISTS V22C0203AuthFixture(
                    TenantId INT NOT NULL, ActorId INT NOT NULL,
                    CommandId INT NOT NULL, MemberActive TINYINT(1) NOT NULL,
                    GrantActive TINYINT(1) NOT NULL, Revoked TINYINT(1) NOT NULL,
                    TenantOwned TINYINT(1) NOT NULL,
                    PRIMARY KEY(TenantId,ActorId)
                ) ENGINE=InnoDB
                """;
            await table.ExecuteNonQueryAsync();
        }
        var a = RandomNumberGenerator.GetInt32(100000, 400000000);
        var b = a + 1;
        var actorA = RandomNumberGenerator.GetInt32(400000001, 900000000);
        var actorB = actorA + 1;
        try
        {
            await SqlAsync(Insert, a, actorA);
            await SqlAsync(Insert, b, actorB);
            using var host = await StartAsync();
            using var client = host.GetTestClient();
            var token = Token(actorA);
            using (var noJwt = await GetAsync(client, a, Rules.Command.Suspend, null))
                Assert.Equal(HttpStatusCode.Unauthorized, noJwt.StatusCode);
            using (var forged = await GetAsync(client, a, Rules.Command.Suspend, Token(actorA, true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var good = await GetAsync(client, a, Rules.Command.Suspend, token))
            {
                Assert.Equal(HttpStatusCode.OK, good.StatusCode);
                Assert.Equal("VERIFIED_READONLY_PREFLIGHT_ONLY", await good.Content.ReadAsStringAsync());
            }
            using (var foreign = await GetAsync(client, b, Rules.Command.Suspend, token))
                Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
            using (var replay = await GetAsync(client, a, Rules.Command.Reactivate, token))
                Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
            await SqlAsync("UPDATE V22C0203AuthFixture SET GrantActive=0 WHERE TenantId=@t AND ActorId=@a", a, actorA);
            using (var noGrant = await GetAsync(client, a, Rules.Command.Suspend, token))
                Assert.Equal(HttpStatusCode.Forbidden, noGrant.StatusCode);
            await SqlAsync("UPDATE V22C0203AuthFixture SET GrantActive=1,Revoked=1 WHERE TenantId=@t AND ActorId=@a", a, actorA);
            using (var revoked = await GetAsync(client, a, Rules.Command.Suspend, token))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
            using (var other = await GetAsync(client, b, Rules.Command.Suspend, Token(actorB)))
                Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        }
        finally
        {
            await SqlAsync(Delete, a, actorA);
            await SqlAsync(Delete, b, actorB);
        }
        var rolledBackTenant = RandomNumberGenerator.GetInt32(100000, 400000000);
        var rolledBackActor = RandomNumberGenerator.GetInt32(400000001, 900000000);
        await using (var db = new MySqlConnection(Connection()))
        {
            await db.OpenAsync();
            await using var tx = await db.BeginTransactionAsync();
            await using (var command = db.CreateCommand())
            {
                command.Transaction = tx;
                command.CommandText = Insert;
                command.Parameters.AddWithValue("@t", rolledBackTenant);
                command.Parameters.AddWithValue("@a", rolledBackActor);
                await command.ExecuteNonQueryAsync();
            }
            await tx.RollbackAsync();
        }
        using var rollbackHost = await StartAsync();
        using var rollbackClient = rollbackHost.GetTestClient();
        using var denied = await GetAsync(rollbackClient, rolledBackTenant,
            Rules.Command.Suspend, Token(rolledBackActor));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }
}
