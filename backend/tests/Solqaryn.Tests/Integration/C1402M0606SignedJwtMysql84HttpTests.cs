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
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// Test-only real signed JWT + HTTP TestServer + ephemeral MySQL 8.4.
/// This does not represent production API DI, real UsuarioEmpresa/RolPermiso,
/// commercial file/job/cache adapters or global P0/P1 security certification.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1402M0606SignedJwtMysql84HttpTests
{
    private const string Issuer = "c1402-m0606-ephemeral-ci";
    private const string Audience = "c1402-m0606-test-client";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c1402-m0606-real-http-mysql84-ci-signing-key-only"));
    private static readonly SymmetricSecurityKey WrongKey = new(
        Encoding.UTF8.GetBytes("c1402-m0606-forged-jwt-signing-key-only-for-test"));
    private static readonly string Sha =
        Environment.GetEnvironmentVariable("GITHUB_SHA") is { Length: 40 } live
            ? live : "0123456789abcdef0123456789abcdef01234567";

    private static string Connection()
    {
        var raw = Environment.GetEnvironmentVariable("C1402_M0606_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var b = new MySqlConnectionStringBuilder(raw);
        if (b.Server != "127.0.0.1" || b.Port != 3306 ||
            b.Database != "c1402_m0606_bootstrap" || b.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");
        return raw;
    }

    private sealed class SqlReader(
        string connection, string table, int actor,
        V5SecurityAcceptanceDomainRules.Surface surface)
        : IC1402SecurityAcceptanceEvidenceReader
    {
        public async ValueTask<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?> ReadAsync(
            string resourceKey, CancellationToken cancellationToken)
        {
            if (actor <= 0) return null;
            await using var db = new MySqlConnection(connection);
            await db.OpenAsync(cancellationToken);
            await using var command = db.CreateCommand();
            command.CommandText = $"""
                SELECT TenantId,OwnerTenantId,BranchId,HostTenantId,
                       MemberActive,GrantActive,SessionActive,AuditActive,
                       Revoked,LinkExpired,ServiceActive,ServiceGrantActive,IdempotencyActive
                FROM {table} WHERE ActorId=@actor AND ResourceKey=@resource
                """;
            command.Parameters.AddWithValue("@actor", actor);
            command.Parameters.AddWithValue("@resource", resourceKey);
            await using var rows = await command.ExecuteReaderAsync(cancellationToken);
            if (!await rows.ReadAsync(cancellationToken)) return null;
            bool Flag(int i) => Convert.ToBoolean(rows.GetValue(i));
            var tenant = rows.GetInt32(0);
            var authority = new V5SecurityAcceptanceDomainRules.VerifiedAuthority(
                actor, tenant, rows.GetInt32(2), Sha,
                true, true, Flag(4), Flag(5), Flag(5), Flag(10), Flag(11));
            var target = new V5SecurityAcceptanceDomainRules.PersistedTarget(
                rows.GetInt32(1), rows.GetInt32(2), rows.GetInt32(3),
                true, true, Flag(8), Flag(9));
            var attempt = new V5SecurityAcceptanceDomainRules.Attempt(
                surface, null, Sha, false, false, Flag(12));
            return new(resourceKey, Sha, true, Flag(6), Flag(4), Flag(5),
                Flag(7), "git-proof:c1402-isolated-mysql", authority, target, attempt);
        }
    }

    private static Task<IHost> StartAsync(string connection, string table) =>
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
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapGet(
                    "/__c1402_test/{resource}/{surface:int}", async http =>
                    {
                        var resource = Convert.ToString(
                            http.Request.RouteValues["resource"]) ?? "";
                        var surface = (V5SecurityAcceptanceDomainRules.Surface)
                            Convert.ToInt32(http.Request.RouteValues["surface"]);
                        int.TryParse(http.User.FindFirst("sub")?.Value, out var actor);
                        var gate = new C1402SecurityAcceptanceAuthorizationGate(
                            new SqlReader(connection, table, actor, surface));
                        var decision = await gate.CheckAsync(
                            new(resource, Sha, surface,
                                ClientControlsTenantOrRole:
                                    http.Request.Headers.ContainsKey("X-Empresa-Id")),
                            http.RequestAborted);
                        http.Response.StatusCode = decision.RequiresSeparateAuthenticatedAdapter
                            ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden;
                        await http.Response.WriteAsync(decision.Code);
                    }).RequireAuthorization());
            })).StartAsync();

    private static string Token(int actor, bool forged = false, bool expired = false) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: new[] { new Claim("sub", actor.ToString()) },
            notBefore: DateTime.UtcNow.AddMinutes(-30),
            expires: DateTime.UtcNow.AddMinutes(expired ? -1 : 10),
            signingCredentials: new SigningCredentials(
                forged ? WrongKey : Key, SecurityAlgorithms.HmacSha256)));

    private static async Task<HttpResponseMessage> RequestAsync(
        HttpClient client, string resource,
        V5SecurityAcceptanceDomainRules.Surface surface,
        string? token, bool spoof = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/__c1402_test/{resource}/{(int)surface}");
        if (token is not null)
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        if (spoof) request.Headers.Add("X-Empresa-Id", "22");
        return await client.SendAsync(request);
    }

    private static async Task ExecuteAsync(string connection, string sql)
    {
        await using var db = new MySqlConnection(connection);
        await db.OpenAsync();
        await using var command = db.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task SignedJwtMysql84TenantABSessionRevocationAndRollbackRemainFailClosed()
    {
        var connection = Connection();
        var table = "V23C1402Security_" + Guid.NewGuid().ToString("N");
        await using (var db = new MySqlConnection(connection))
        {
            await db.OpenAsync();
            await using var version = db.CreateCommand();
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync()));
        }
        await ExecuteAsync(connection, $"""
            CREATE TABLE {table} (
                ActorId INT NOT NULL,
                ResourceKey VARCHAR(96) NOT NULL,
                TenantId INT NOT NULL,
                OwnerTenantId INT NOT NULL,
                BranchId INT NOT NULL,
                HostTenantId INT NOT NULL,
                MemberActive BOOLEAN NOT NULL,
                GrantActive BOOLEAN NOT NULL,
                SessionActive BOOLEAN NOT NULL,
                AuditActive BOOLEAN NOT NULL,
                Revoked BOOLEAN NOT NULL,
                LinkExpired BOOLEAN NOT NULL,
                ServiceActive BOOLEAN NOT NULL,
                ServiceGrantActive BOOLEAN NOT NULL,
                IdempotencyActive BOOLEAN NOT NULL,
                PRIMARY KEY(ActorId,ResourceKey)
            ) ENGINE=InnoDB
            """);
        try
        {
            await ExecuteAsync(connection, $"""
                INSERT INTO {table} VALUES
                (101,'resource-A',11,11,7,11,1,1,1,1,0,0,0,0,0),
                (202,'resource-B',22,22,8,22,1,1,1,1,0,0,0,0,0)
                """);
            using var host = await StartAsync(connection, table);
            using var client = host.GetTestClient();
            var a = Token(101);
            var b = Token(202);
            const V5SecurityAcceptanceDomainRules.Surface read =
                V5SecurityAcceptanceDomainRules.Surface.BusinessRead;
            using (var anonymous = await RequestAsync(client, "resource-A", read, null))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            using (var forged = await RequestAsync(client, "resource-A", read, Token(101, forged: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
            using (var expired = await RequestAsync(client, "resource-A", read, Token(101, expired: true)))
                Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
            using (var allowed = await RequestAsync(client, "resource-A", read, a))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED",
                    await allowed.Content.ReadAsStringAsync());
            }
            using (var foreign = await RequestAsync(client, "resource-B", read, a))
                Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
            using (var other = await RequestAsync(client, "resource-B", read, b))
                Assert.Equal(HttpStatusCode.OK, other.StatusCode);
            using (var spoof = await RequestAsync(client, "resource-A", read, a, spoof: true))
                Assert.Equal(HttpStatusCode.Forbidden, spoof.StatusCode);

            foreach (var column in new[] { "SessionActive", "MemberActive", "GrantActive", "AuditActive" })
            {
                await ExecuteAsync(connection,
                    $"UPDATE {table} SET {column}=0 WHERE ActorId=101");
                using (var denied = await RequestAsync(client, "resource-A", read, a))
                    Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
                await ExecuteAsync(connection,
                    $"UPDATE {table} SET {column}=1 WHERE ActorId=101");
            }
            await ExecuteAsync(connection, $"UPDATE {table} SET Revoked=1 WHERE ActorId=101");
            using (var revoked = await RequestAsync(client, "resource-A", read, a))
                Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
            await ExecuteAsync(connection, $"UPDATE {table} SET Revoked=0 WHERE ActorId=101");

            await ExecuteAsync(connection, $"UPDATE {table} SET HostTenantId=22 WHERE ActorId=101");
            using (var foreignHost = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.HostResolve, a))
                Assert.Equal(HttpStatusCode.Forbidden, foreignHost.StatusCode);
            await ExecuteAsync(connection, $"UPDATE {table} SET HostTenantId=11 WHERE ActorId=101");
            await ExecuteAsync(connection, $"UPDATE {table} SET LinkExpired=1 WHERE ActorId=101");
            using (var expiredLink = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.FileDownload, a))
                Assert.Equal(HttpStatusCode.Forbidden, expiredLink.StatusCode);
            using (var unauthorizedJob = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.BackgroundJob, a))
                Assert.Equal(HttpStatusCode.Forbidden, unauthorizedJob.StatusCode);

            // New causal negative: a valid service actor is not an explicit
            // service grant. These flags were previously both read from
            // ServiceActive, allowing the fixture to mask absent grants.
            await ExecuteAsync(connection, $"""
                UPDATE {table}
                SET ServiceActive=1, ServiceGrantActive=0, IdempotencyActive=1
                WHERE ActorId=101
                """);
            using (var absentServiceGrant = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.BackgroundJob, a))
                Assert.Equal(HttpStatusCode.Forbidden, absentServiceGrant.StatusCode);
            await ExecuteAsync(connection,
                $"UPDATE {table} SET ServiceGrantActive=1 WHERE ActorId=101");
            using (var allServerConditions = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.BackgroundJob, a))
            {
                Assert.Equal(HttpStatusCode.OK, allServerConditions.StatusCode);
                Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED",
                    await allServerConditions.Content.ReadAsStringAsync());
            }
            await ExecuteAsync(connection,
                $"UPDATE {table} SET ServiceActive=0 WHERE ActorId=101");
            using (var missingServiceIdentity = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.BackgroundJob, a))
                Assert.Equal(HttpStatusCode.Forbidden, missingServiceIdentity.StatusCode);
            await ExecuteAsync(connection, $"""
                UPDATE {table} SET ServiceActive=1, IdempotencyActive=0
                WHERE ActorId=101
                """);
            using (var missingIdempotency = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.BackgroundJob, a))
                Assert.Equal(HttpStatusCode.Forbidden, missingIdempotency.StatusCode);
            using (var restore = await RequestAsync(client, "resource-A",
                V5SecurityAcceptanceDomainRules.Surface.BackupRestore, a))
                Assert.Equal(HttpStatusCode.Forbidden, restore.StatusCode);
            using (var cacheForeign = await RequestAsync(client, "resource-B",
                V5SecurityAcceptanceDomainRules.Surface.CacheRead, a))
                Assert.Equal(HttpStatusCode.Forbidden, cacheForeign.StatusCode);

            await using (var db = new MySqlConnection(connection))
            {
                await db.OpenAsync();
                await using var tx = await db.BeginTransactionAsync();
                await using var command = db.CreateCommand();
                command.Transaction = tx;
                command.CommandText = $"""
                    INSERT INTO {table} VALUES
                    (101,'resource-rollback',11,11,7,11,1,1,1,1,0,0,0,0,0)
                    """;
                await command.ExecuteNonQueryAsync();
                await tx.RollbackAsync();
            }
            using (var rolledBack = await RequestAsync(client, "resource-rollback", read, a))
                Assert.Equal(HttpStatusCode.Forbidden, rolledBack.StatusCode);
        }
        finally
        {
            await ExecuteAsync(connection, $"DROP TABLE IF EXISTS {table}");
        }
    }

    /// <summary>
    /// Distinct negative: a previously accepted signed-JWT preflight must not
    /// cache the decision if its ephemeral trusted MySQL reader disappears.
    /// This exercises only TestServer/fixture, not production API adapters.
    /// </summary>
    [Fact]
    public async Task SignedJwtMysql84SqlReaderOutageAfterAllowedRequestFailsClosed()
    {
        var connection = Connection();
        var table = "V23C1402Outage_" + Guid.NewGuid().ToString("N");
        await ExecuteAsync(connection, $"""
            CREATE TABLE {table} (
                ActorId INT NOT NULL,
                ResourceKey VARCHAR(96) NOT NULL,
                TenantId INT NOT NULL,
                OwnerTenantId INT NOT NULL,
                BranchId INT NOT NULL,
                HostTenantId INT NOT NULL,
                MemberActive BOOLEAN NOT NULL,
                GrantActive BOOLEAN NOT NULL,
                SessionActive BOOLEAN NOT NULL,
                AuditActive BOOLEAN NOT NULL,
                Revoked BOOLEAN NOT NULL,
                LinkExpired BOOLEAN NOT NULL,
                ServiceActive BOOLEAN NOT NULL,
                ServiceGrantActive BOOLEAN NOT NULL,
                IdempotencyActive BOOLEAN NOT NULL,
                PRIMARY KEY(ActorId, ResourceKey)
            ) ENGINE=InnoDB
            """);
        try
        {
            await ExecuteAsync(connection, $"""
                INSERT INTO {table} VALUES
                (101,'resource-A',11,11,7,11,1,1,1,1,0,0,0,0,0)
                """);
            using var host = await StartAsync(connection, table);
            using var client = host.GetTestClient();
            var signedJwt = Token(101);
            const V5SecurityAcceptanceDomainRules.Surface read =
                V5SecurityAcceptanceDomainRules.Surface.BusinessRead;

            using (var beforeOutage = await RequestAsync(client, "resource-A", read, signedJwt))
            {
                Assert.Equal(HttpStatusCode.OK, beforeOutage.StatusCode);
                Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED",
                    await beforeOutage.Content.ReadAsStringAsync());
            }

            // Same valid bearer, same HTTP resource: remove only the isolated
            // SQL table so a prior 'ready' preflight cannot bypass fresh state.
            await ExecuteAsync(connection, $"DROP TABLE {table}");
            using (var afterOutage = await RequestAsync(client, "resource-A", read, signedJwt))
            {
                Assert.Equal(HttpStatusCode.Forbidden, afterOutage.StatusCode);
                Assert.Equal("TRUSTED_EVIDENCE_SOURCE_UNAVAILABLE",
                    await afterOutage.Content.ReadAsStringAsync());
            }

            // No success even for another actor or a spoofed client tenant.
            using (var otherActor = await RequestAsync(client, "resource-A", read, Token(202)))
                Assert.Equal(HttpStatusCode.Forbidden, otherActor.StatusCode);
            using (var spoof = await RequestAsync(client, "resource-A", read, signedJwt, spoof: true))
                Assert.Equal(HttpStatusCode.Forbidden, spoof.StatusCode);
        }
        finally
        {
            await ExecuteAsync(connection, $"DROP TABLE IF EXISTS {table}");
        }
    }

}
