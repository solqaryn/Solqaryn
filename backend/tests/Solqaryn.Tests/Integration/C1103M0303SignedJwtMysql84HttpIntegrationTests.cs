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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.C1103OutboxAuthorizationGate;
using Rules = Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// Real JWT middleware, HTTP and disposable MySQL 8.4 integration for C11-03.M03.03.
/// The SQL fixture and test endpoint are NOT a production membership adapter.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0303SignedJwtMysql84HttpIntegrationTests
{
    private const string DbName = "solqaryn_c1502_m0503";
    private const string Issuer = "solqaryn-c1103-v22-test";
    private const string Audience = "solqaryn-c1103-v22-client";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("v22-c1103-test-only-valid-signing-key-for-mysql84-2026"));
    private static readonly SymmetricSecurityKey ForgedKey = new(
        Encoding.UTF8.GetBytes("v22-c1103-test-only-forged-signing-key-for-mysql84-2026"));

    private static string Connection()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("MYSQL84_EPHEMERAL_TEST_CONNECTION_REQUIRED");
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server != "127.0.0.1" || parsed.Port != 3306 ||
            parsed.Database != DbName || parsed.UserID != "root" ||
            parsed.SslMode != MySqlSslMode.Disabled)
            throw new InvalidOperationException("MYSQL84_EPHEMERAL_LOOPBACK_REQUIRED");
        return raw;
    }

    private static async Task SetupAsync()
    {
        await using var db = new MySqlConnection(Connection());
        await db.OpenAsync();
        await using (var version = db.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
        }
        await using var create = db.CreateCommand();
        create.CommandText = """
            CREATE TABLE IF NOT EXISTS V22C1103AuthFixture(
                TenantId INT NOT NULL, ActorId INT NOT NULL,
                UserActive TINYINT(1) NOT NULL, MemberActive TINYINT(1) NOT NULL,
                GrantActive TINYINT(1) NOT NULL, Revoked TINYINT(1) NOT NULL,
                TenantOwned TINYINT(1) NOT NULL, EventAllowed TINYINT(1) NOT NULL,
                PRIMARY KEY(TenantId,ActorId)
            ) ENGINE=InnoDB
            """;
        await create.ExecuteNonQueryAsync();
    }

    private static async Task SqlAsync(string sql, int tenant, int actor)
    {
        await using var db = new MySqlConnection(Connection());
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@tenant", tenant);
        cmd.Parameters.AddWithValue("@actor", actor);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed class FixtureReader : IC1103OutboxCurrentMembershipReader
    {
        public async ValueTask<Gate.CurrentAuthority?> ReadCurrentAsync(
            int actor, int tenant, CancellationToken ct)
        {
            await using var db = new MySqlConnection(Connection());
            await db.OpenAsync(ct);
            await using var cmd = db.CreateCommand();
            cmd.CommandText = """
                SELECT UserActive,MemberActive,GrantActive,Revoked,TenantOwned,EventAllowed
                FROM V22C1103AuthFixture WHERE TenantId=@tenant AND ActorId=@actor
                """;
            cmd.Parameters.AddWithValue("@tenant", tenant);
            cmd.Parameters.AddWithValue("@actor", actor);
            await using var rows = await cmd.ExecuteReaderAsync(ct);
            if (!await rows.ReadAsync(ct)) return null;
            bool F(string name) => Convert.ToBoolean(rows.GetValue(rows.GetOrdinal(name)));
            return new Gate.CurrentAuthority(
                actor, tenant, true, F("UserActive") && F("MemberActive"),
                F("GrantActive"), F("Revoked"), F("TenantOwned"), DateTime.UtcNow,
                F("EventAllowed")
                    ? new HashSet<string>(StringComparer.Ordinal) { "ventas.factura.emitida" }
                    : new HashSet<string>(StringComparer.Ordinal));
        }
    }

    private static Task<IHost> HostAsync() =>
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
                            ValidateIssuerSigningKey = true, IssuerSigningKey = Key,
                            RequireSignedTokens = true, RequireExpirationTime = true
                        };
                    });
                services.AddAuthorization();
            }).Configure(app =>
            {
                app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints
                    .MapGet("/__v22_test/outbox/{tenant:int}", async http =>
                    {
                        var tenant = int.Parse(Convert.ToString(http.Request.RouteValues["tenant"])!);
                        var draft = new Rules.EventDraft(
                            tenant, Guid.Parse("11111111-2222-4333-8444-555555555555"),
                            "c1103-test-only-idempotency-123456789",
                            Convert.ToString(http.Request.Query["eventType"]),
                            new string('a', 64), DateTime.UtcNow.AddMinutes(-1),
                            "c1103-v22", 1, 0, 3);
                        var verdict = await new Gate(new FixtureReader())
                            .InspectAsync(http.User, draft, http.RequestAborted);
                        http.Response.StatusCode = verdict.PermitsDomainPreflightOnly
                            ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden;
                        await http.Response.WriteAsync(verdict.Code);
                    }).RequireAuthorization());
            })).StartAsync();

    private static string Token(int user, bool valid = true)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, user.ToString()) },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(valid ? Key : ForgedKey,
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<HttpResponseMessage> ReadAsync(
        HttpClient client, int tenant, string? token, string eventType = "ventas.factura.emitida")
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            "/__v22_test/outbox/" + tenant + "?eventType=" + Uri.EscapeDataString(eventType));
        if (token is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Empresa-Id", "999999");
        return await client.SendAsync(req);
    }

    private const string Insert = """
        INSERT INTO V22C1103AuthFixture
        (TenantId,ActorId,UserActive,MemberActive,GrantActive,Revoked,TenantOwned,EventAllowed)
        VALUES(@tenant,@actor,1,1,1,0,1,1)
        """;
    private const string Delete = """
        DELETE FROM V22C1103AuthFixture WHERE TenantId=@tenant AND ActorId=@actor
        """;

    [Fact]
    public async Task SignedJwtHttpMysql84EnforcesTenantABRevocationAndEventAllowlist()
    {
        await SetupAsync();
        const int a = 811, b = 812, userA = 911, userB = 912;
        await SqlAsync(Delete, a, userA);
        await SqlAsync(Delete, b, userB);
        try
        {
            await SqlAsync(Insert, a, userA);
            await SqlAsync(Insert, b, userB);
            using var host = await HostAsync();
            using var client = host.GetTestClient();
            var jwtA = Token(userA);
            using (var missing = await ReadAsync(client, a, null))
                Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
            using (var forged = await ReadAsync(client, a, Token(userA, false)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var allowed = await ReadAsync(client, a, jwtA))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Equal("OUTBOX_AUTH_PREFLIGHT_ONLY",
                    await allowed.Content.ReadAsStringAsync());
            }
            using (var foreign = await ReadAsync(client, b, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
            using (var wrongActor = await ReadAsync(client, a, Token(userB)))
                Assert.Equal(HttpStatusCode.Forbidden, wrongActor.StatusCode);
            using (var forbiddenEvent = await ReadAsync(client, a, jwtA, "admin.secrets.export"))
                Assert.Equal(HttpStatusCode.Forbidden, forbiddenEvent.StatusCode);
            await SqlAsync("UPDATE V22C1103AuthFixture SET MemberActive=0 WHERE TenantId=@tenant AND ActorId=@actor", a, userA);
            using (var revokedMember = await ReadAsync(client, a, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, revokedMember.StatusCode);
            await SqlAsync("UPDATE V22C1103AuthFixture SET MemberActive=1,GrantActive=0 WHERE TenantId=@tenant AND ActorId=@actor", a, userA);
            using (var missingGrant = await ReadAsync(client, a, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, missingGrant.StatusCode);
            await SqlAsync("UPDATE V22C1103AuthFixture SET GrantActive=1,UserActive=0 WHERE TenantId=@tenant AND ActorId=@actor", a, userA);
            using (var disabledUser = await ReadAsync(client, a, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, disabledUser.StatusCode);
            await SqlAsync("UPDATE V22C1103AuthFixture SET UserActive=1,Revoked=1 WHERE TenantId=@tenant AND ActorId=@actor", a, userA);
            using (var revoked = await ReadAsync(client, a, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
            using (var otherTenant = await ReadAsync(client, b, Token(userB)))
                Assert.Equal(HttpStatusCode.OK, otherTenant.StatusCode);
        }
        finally
        {
            await SqlAsync(Delete, a, userA);
            await SqlAsync(Delete, b, userB);
        }
    }

    [Fact]
    public async Task MysqlRollbackNeverBecomesAuthorityForSignedJwtPreflight()
    {
        await SetupAsync();
        const int tenant = 813, actor = 913;
        await SqlAsync(Delete, tenant, actor);
        await using (var db = new MySqlConnection(Connection()))
        {
            await db.OpenAsync();
            await using var tx = await db.BeginTransactionAsync();
            await using (var cmd = db.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = Insert;
                cmd.Parameters.AddWithValue("@tenant", tenant);
                cmd.Parameters.AddWithValue("@actor", actor);
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.RollbackAsync();
        }
        using var host = await HostAsync();
        using var client = host.GetTestClient();
        using var denied = await ReadAsync(client, tenant, Token(actor));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }
}
