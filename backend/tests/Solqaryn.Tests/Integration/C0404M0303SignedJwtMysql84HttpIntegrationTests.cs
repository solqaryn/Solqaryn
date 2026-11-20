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
using Gate = Solqaryn.Application.AutomationV5.C0404PaymentAuthorizationGate;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// V22: real JWT signature, HTTP TestServer and ephemeral MySQL 8.4 tests.
/// Fixture-only SQL is NOT a production AppDbContext or DI authorization adapter.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0404M0303SignedJwtMysql84HttpIntegrationTests
{
    private const string Db = "solqaryn_c1502_m0503";
    private const string Issuer = "solqaryn-c0404-ephemeral-v22";
    private const string Audience = "solqaryn-c0404-ephemeral-client";
    private static readonly SymmetricSecurityKey Key =
        new(Encoding.UTF8.GetBytes("v22-test-only-c0404-mysql84-jwt-valid-key-2026-very-long"));
    private static readonly SymmetricSecurityKey InvalidKey =
        new(Encoding.UTF8.GetBytes("v22-test-only-c0404-mysql84-jwt-invalid-key-very-long"));

    private static string Connection()
    {
        var raw = Environment.GetEnvironmentVariable("C1502_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("MYSQL84_EPHEMERAL_TEST_CONNECTION_REQUIRED");
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server != "127.0.0.1" || parsed.Port != 3306 ||
            parsed.Database != Db || parsed.UserID != "root" ||
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
            CREATE TABLE IF NOT EXISTS V22C0404AuthFixture(
                TenantId INT NOT NULL, InvoiceId INT NOT NULL, ActorId INT NOT NULL,
                UserActive TINYINT(1) NOT NULL, MembershipActive TINYINT(1) NOT NULL,
                GrantAllowed TINYINT(1) NOT NULL, Revoked TINYINT(1) NOT NULL,
                PRIMARY KEY(TenantId, InvoiceId, ActorId)
            ) ENGINE=InnoDB
            """;
        await create.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(string sql, int tenant, int invoice, int actor = 0, int flag = 0)
    {
        await using var db = new MySqlConnection(Connection());
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@tenant", tenant);
        cmd.Parameters.AddWithValue("@invoice", invoice);
        cmd.Parameters.AddWithValue("@actor", actor);
        cmd.Parameters.AddWithValue("@flag", flag);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed class EphemeralReader(IHttpContextAccessor accessor)
        : IC0404PaymentAuthorizationEvidenceReader
    {
        public async ValueTask<Gate.TrustedSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            var http = accessor.HttpContext;
            if (http?.User.Identity?.IsAuthenticated != true ||
                !int.TryParse(http.User.FindFirst("id")?.Value, out var actor) ||
                !int.TryParse(Convert.ToString(http.Request.RouteValues["tenant"]), out var tenant) ||
                !int.TryParse(Convert.ToString(http.Request.RouteValues["invoice"]), out var invoice) ||
                actor <= 0 || tenant <= 0 || invoice <= 0) return null;
            await using var db = new MySqlConnection(Connection());
            await db.OpenAsync(ct);
            await using var cmd = db.CreateCommand();
            cmd.CommandText = """
                SELECT UserActive, MembershipActive, GrantAllowed, Revoked
                FROM V22C0404AuthFixture
                WHERE TenantId=@tenant AND InvoiceId=@invoice AND ActorId=@actor
                """;
            cmd.Parameters.AddWithValue("@tenant", tenant);
            cmd.Parameters.AddWithValue("@invoice", invoice);
            cmd.Parameters.AddWithValue("@actor", actor);
            await using var rows = await cmd.ExecuteReaderAsync(ct);
            if (!await rows.ReadAsync(ct)) return null;
            bool Value(string field) => Convert.ToBoolean(rows.GetValue(rows.GetOrdinal(field)));
            var grant = Value("GrantAllowed");
            return new Gate.TrustedSnapshot
            {
                ActorId = actor, TenantId = tenant, InvoiceId = invoice,
                JwtSignatureIssuerAudienceLifetimeVerifiedByBackend = true,
                CurrentSessionVerified = Value("UserActive"),
                UserActive = Value("UserActive"), AuthorityDatabaseAvailable = true,
                MembershipFromPersistence = true, MembershipActive = Value("MembershipActive"),
                RoleAndGrantFromPersistence = grant, PermissionRevoked = Value("Revoked"),
                TenantReadFencedByDatabase = true,
                InvoiceOwnershipVerifiedFromPersistence = true,
                AuthorizationRevisionFresh = true, FeatureEnabledByTrustedConfig = true,
                CreateGrant = grant, RefundGrant = false, ReceiptAndLedgerVersionVerified = false
            };
        }
    }

    private static Task<IHost> HostAsync() =>
        new HostBuilder().ConfigureWebHost(web =>
            web.UseTestServer().ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options => options.TokenValidationParameters =
                            new TokenValidationParameters
                            {
                                ValidateIssuer = true, ValidIssuer = Issuer,
                                ValidateAudience = true, ValidAudience = Audience,
                                ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                                ValidateIssuerSigningKey = true, IssuerSigningKey = Key
                            });
                    services.AddAuthorization();
                }).Configure(app =>
                {
                    app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                        endpoints.MapGet("/__v22_test/payment/{tenant:int}/{invoice:int}",
                            async http =>
                            {
                                var tenant = int.Parse(Convert.ToString(http.Request.RouteValues["tenant"])!);
                                var invoice = int.Parse(Convert.ToString(http.Request.RouteValues["invoice"])!);
                                var reader = new EphemeralReader(
                                    http.RequestServices.GetRequiredService<IHttpContextAccessor>());
                                var decision = await new Gate(reader).InspectAsync(
                                    new Gate.Request(tenant, invoice, Gate.Operation.CreateLink),
                                    http.RequestAborted);
                                http.Response.StatusCode = decision.MayInspectPureApplicationPlan
                                    ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden;
                                await http.Response.WriteAsync(decision.Code);
                            }).RequireAuthorization());
                })).StartAsync();

    private static string Token(int actor, bool valid = true)
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: new[] { new Claim("id", actor.ToString()) },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(valid ? Key : InvalidKey,
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static async Task<HttpResponseMessage> ReadAsync(
        HttpClient client, int tenant, int invoice, string? token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            "/__v22_test/payment/" + tenant + "/" + invoice);
        if (token is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Empresa-Id", "999999");
        return await client.SendAsync(req);
    }

    [Fact]
    public async Task RealJwtHttpMysql84TenantABAndRevocationCannotEscapeTenant()
    {
        await SetupAsync();
        var invoice = RandomNumberGenerator.GetInt32(100000, int.MaxValue - 1);
        const int tenantA = 401, tenantB = 402, actorA = 701, actorB = 702;
        const string insert = """
            INSERT INTO V22C0404AuthFixture
            (TenantId,InvoiceId,ActorId,UserActive,MembershipActive,GrantAllowed,Revoked)
            VALUES (@tenant,@invoice,@actor,1,1,1,0)
            """;
        try
        {
            await ExecuteAsync(insert, tenantA, invoice, actorA);
            await ExecuteAsync(insert, tenantB, invoice, actorB);
            using var host = await HostAsync();
            using var client = host.GetTestClient();
            var jwtA = Token(actorA);
            using (var missing = await ReadAsync(client, tenantA, invoice, null))
                Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
            using (var forged = await ReadAsync(client, tenantA, invoice, Token(actorA, false)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var allowed = await ReadAsync(client, tenantA, invoice, jwtA))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Equal("READONLY_PAYMENT_PLAN_PREFLIGHT_ONLY",
                    await allowed.Content.ReadAsStringAsync());
            }
            using (var crossA = await ReadAsync(client, tenantB, invoice, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, crossA.StatusCode);
            using (var crossB = await ReadAsync(client, tenantA, invoice, Token(actorB)))
                Assert.Equal(HttpStatusCode.Forbidden, crossB.StatusCode);
            using (var foreign = await ReadAsync(client, tenantA, invoice + 1, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
            await ExecuteAsync("UPDATE V22C0404AuthFixture SET MembershipActive=0 WHERE TenantId=@tenant AND InvoiceId=@invoice",
                tenantA, invoice);
            using (var revokedMember = await ReadAsync(client, tenantA, invoice, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, revokedMember.StatusCode);
            await ExecuteAsync("UPDATE V22C0404AuthFixture SET MembershipActive=1,GrantAllowed=0 WHERE TenantId=@tenant AND InvoiceId=@invoice",
                tenantA, invoice);
            using (var noGrant = await ReadAsync(client, tenantA, invoice, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, noGrant.StatusCode);
            await ExecuteAsync("UPDATE V22C0404AuthFixture SET GrantAllowed=1,UserActive=0 WHERE TenantId=@tenant AND InvoiceId=@invoice",
                tenantA, invoice);
            using (var disabled = await ReadAsync(client, tenantA, invoice, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, disabled.StatusCode);
            await ExecuteAsync("UPDATE V22C0404AuthFixture SET UserActive=1,Revoked=1 WHERE TenantId=@tenant AND InvoiceId=@invoice",
                tenantA, invoice);
            using (var revokedGrant = await ReadAsync(client, tenantA, invoice, jwtA))
                Assert.Equal(HttpStatusCode.Forbidden, revokedGrant.StatusCode);
        }
        finally
        {
            await ExecuteAsync("DELETE FROM V22C0404AuthFixture WHERE InvoiceId=@invoice",
                tenantA, invoice);
        }
    }

    [Fact]
    public async Task RealMysqlRollbackDoesNotAuthorizeSignedJwt()
    {
        await SetupAsync();
        var invoice = RandomNumberGenerator.GetInt32(100000, int.MaxValue - 1);
        await using (var db = new MySqlConnection(Connection()))
        {
            await db.OpenAsync();
            await using var trx = await db.BeginTransactionAsync();
            await using (var cmd = db.CreateCommand())
            {
                cmd.Transaction = trx;
                cmd.CommandText = """
                    INSERT INTO V22C0404AuthFixture
                    (TenantId,InvoiceId,ActorId,UserActive,MembershipActive,GrantAllowed,Revoked)
                    VALUES (403,@invoice,703,1,1,1,0)
                    """;
                cmd.Parameters.AddWithValue("@invoice", invoice);
                await cmd.ExecuteNonQueryAsync();
            }
            await trx.RollbackAsync();
        }
        using var host = await HostAsync();
        using var client = host.GetTestClient();
        using var denied = await ReadAsync(client, 403, invoice, Token(703));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }
}
