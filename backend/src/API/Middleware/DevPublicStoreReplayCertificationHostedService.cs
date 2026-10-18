using System.Data;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Middleware;

/// <summary>
/// Explicitly gated, one-shot DEV/Render live replay certification.
/// Sends only GET /tienda/binding to the same Kestrel instance over loopback.
/// Never exposes proof material in a URL, log, response artifact or GitHub secret.
/// This is not a public diagnostic endpoint or a production feature.
/// </summary>
public sealed class DevPublicStoreReplayCertificationHostedService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    ILogger<DevPublicStoreReplayCertificationHostedService> logger) : BackgroundService
{
    private const string AllowedDbHost = "solqaryn-mysql-solqaryn.h.aivencloud.com";
    private const string PublicHost = "solqaryn-dev.vercel.app";
    private const string RenderApiHost = "solqaryn-api-dev-fxx8.onrender.com";
    private const string StoreKey = "tienda-a";
    private const string BindingPath = "/tienda/binding";
    private const string Flag = "PublicStore:ReplayLiveCertificationOnStartup";

    public static bool CanRegister(bool isRender, bool isDevelopment, IConfiguration settings) =>
        isRender && isDevelopment && settings.GetValue<bool>(Flag);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CertifyAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Deployment stopped; absence of a PASS is never a certification.
            logger.LogWarning("PUBLIC_STORE_HMAC_REPLAY_LIVE=BLOCKED reason=Shutdown");
        }
        catch (Exception exception)
        {
            // Never log message, stack trace, SQL parameters, headers, nonce or secret.
            logger.LogWarning(
                "PUBLIC_STORE_HMAC_REPLAY_LIVE=BLOCKED reason={Reason}",
                exception.GetType().Name);
        }
    }

    private async Task CertifyAsync(CancellationToken token)
    {
        // The flag alone is not authority: runtime, server, DB and tenant are pinned.
        if (!hostEnvironment.IsDevelopment() ||
            !string.Equals(Environment.GetEnvironmentVariable("RENDER"), "true",
                StringComparison.OrdinalIgnoreCase) ||
            !configuration.GetValue<bool>(Flag) ||
            !configuration.GetValue<bool>("PublicStore:EnableMultiEmpresaBinding") ||
            !configuration.GetValue<bool>("PublicStore:EnableDatabaseBindings") ||
            !configuration.GetValue<bool>("PublicStore:RequireVerifiedProxyProof"))
            throw new InvalidOperationException("DEV replay certification gates not satisfied.");

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DEV MySQL binding required.");
        var connection = new MySqlConnectionStringBuilder(connectionString);
        if (connection.Server != AllowedDbHost ||
            connection.Port != 14402 ||
            connection.Database != "solqaryn_dev" ||
            connection.UserID != "solqaryn_dev_user" ||
            !string.Equals(connection.SslMode.ToString(), "Required",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsafe DEV MySQL binding.");

        var secret = configuration["PublicStore:ProxySigningKey"];
        if (string.IsNullOrEmpty(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("DEV proxy signing key is unavailable.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var companyId = await db.PublicStoreBindings.IgnoreQueryFilters().AsNoTracking()
            .Where(binding => binding.Activa &&
                binding.Host == PublicHost && binding.StoreKey == StoreKey)
            .Join(db.Set<Empresa>().IgnoreQueryFilters().AsNoTracking(),
                binding => binding.EmpresaId, empresa => empresa.Id,
                (binding, empresa) => new { empresa.Id, empresa.Activa })
            .Where(empresa => empresa.Activa)
            .Select(empresa => (int?)empresa.Id)
            .SingleOrDefaultAsync(token);
        if (companyId is not > 0)
            throw new InvalidOperationException("DEV test tenant is not active and uniquely bound.");

        var rawPort = Environment.GetEnvironmentVariable("PORT");
        var port = string.IsNullOrWhiteSpace(rawPort) ? 10000 :
            int.TryParse(rawPort, NumberStyles.None, CultureInfo.InvariantCulture,
                out var parsed) && parsed is > 0 and <= 65535 ? parsed : 0;
        if (port == 0)
            throw new InvalidOperationException("Invalid local Render port.");

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        var loopback = new Uri($"http://127.0.0.1:{port}");
        await WaitForLocalApiAsync(client, loopback, token);
        // A new Render instance can be reachable before deploy hand-off finishes.
        // Keep this one-shot DEV probe out of the initialization/health window.
        await Task.Delay(TimeSpan.FromSeconds(45), token);

        // The same exact v2 signature, timestamp and nonce MUST be sent twice.
        var firstProof = Sign(secret);
        var first = await CallBindingAsync(loopback, firstProof, companyId.Value, token);
        var ledgerAfterFirst = await CountDigestAsync(db, firstProof.Proof, token);
        if (first != HttpStatusCode.OK || ledgerAfterFirst != 1)
            throw new InvalidOperationException("First signed request was not accepted and persisted.");
        logger.LogInformation("PUBLIC_STORE_HMAC_REPLAY_STAGE=FIRST_PASS env=DEV http=200 ledger=PASS");

        var repeated = await CallBindingAsync(loopback, firstProof, companyId.Value, token);
        var ledgerAfterReplay = await CountDigestAsync(db, firstProof.Proof, token);
        if (repeated != HttpStatusCode.ServiceUnavailable || ledgerAfterReplay != 1)
            throw new InvalidOperationException("Repeated proof was not blocked by shared ledger.");
        logger.LogInformation("PUBLIC_STORE_HMAC_REPLAY_STAGE=REPLAY_PASS env=DEV http=503 ledger=PASS");

        // Fresh nonce and proof must still work for the SAME legitimate tenant.
        // Retry only transient transport failures, always with a NEW nonce/proof.
        // A security rejection or an unexpected ledger state is never retryable.
        var freshVerified = false;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            token.ThrowIfCancellationRequested();
            var freshProof = Sign(secret);
            logger.LogInformation("PUBLIC_STORE_HMAC_REPLAY_STAGE=FRESH_ATTEMPT env=DEV attempt={Attempt}", attempt);
            try
            {
                var fresh = await CallBindingAsync(loopback, freshProof, companyId.Value, token);
                var freshLedger = await CountDigestAsync(db, freshProof.Proof, token);
                if (fresh != HttpStatusCode.OK || freshLedger != 1 ||
                    string.Equals(freshProof.Proof, firstProof.Proof, StringComparison.Ordinal))
                    throw new InvalidOperationException("Fresh signed request failed after replay rejection.");

                freshVerified = true;
                break;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested && attempt < 3)
            {
                logger.LogWarning("PUBLIC_STORE_HMAC_REPLAY_STAGE=FRESH_TRANSPORT_RETRY env=DEV attempt={Attempt}", attempt);
            }
            catch (HttpRequestException) when (attempt < 3)
            {
                logger.LogWarning("PUBLIC_STORE_HMAC_REPLAY_STAGE=FRESH_TRANSPORT_RETRY env=DEV attempt={Attempt}", attempt);
            }
        }

        if (!freshVerified)
            throw new InvalidOperationException("Fresh signed request did not finish with valid readback.");
        logger.LogInformation("PUBLIC_STORE_HMAC_REPLAY_STAGE=FRESH_PASS env=DEV http=200 ledger=PASS");

        var commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT") ?? "unknown";
        if (commit.Length != 40 || commit.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
            commit = "unknown";
        logger.LogInformation(
            "PUBLIC_STORE_HMAC_REPLAY_LIVE=PASS env=DEV first=200 replay=503 fresh=200 ledger=PASS company_binding=PASS commit={Commit}",
            commit);
    }

    private static async Task WaitForLocalApiAsync(HttpClient client, Uri loopback,
        CancellationToken token)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var health = new HttpRequestMessage(HttpMethod.Get,
                    new Uri(loopback, "/health/ready"));
                // Kestrel allows only its pinned DEV Render Host. Connect to loopback
                // but preserve HostFilteringMiddleware without widening AllowedHosts.
                health.Headers.Host = RenderApiHost;
                health.Headers.ConnectionClose = true;
                using var response = await client.SendAsync(health, token);
                if (response.StatusCode == HttpStatusCode.OK)
                    return;
            }
            catch (HttpRequestException)
            {
                // Kestrel may not be listening yet.
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
                // Bound retries, with no signed requests or changes.
            }
            await Task.Delay(TimeSpan.FromSeconds(1), token);
        }
        throw new InvalidOperationException("DEV local API never became ready.");
    }

    private sealed record SignedProof(string At, string Nonce, string Proof);

    private static SignedProof Sign(string secret)
    {
        var at = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
            .ToLowerInvariant();
        var canonical = $"v2\n{at}\nGET\n{BindingPath}\n{PublicHost}\n{StoreKey}\n{nonce}";
        var digest = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonical));
        return new SignedProof(at, nonce, Convert.ToHexString(digest).ToLowerInvariant());
    }

    private static async Task<HttpStatusCode> CallBindingAsync(
        Uri loopback, SignedProof proof, int expectedCompany, CancellationToken token)
    {
        // Each proof gets its own transport; a hung response never contaminates
        // the next proof or shares a stale socket during Render startup.
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(16) };
        using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        requestTimeout.CancelAfter(TimeSpan.FromSeconds(14));
        using var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri(loopback, BindingPath));
        // Use the allowed DEV API Host over a loopback TCP connection.
        request.Headers.Host = RenderApiHost;
        request.Headers.ConnectionClose = true;
        request.Headers.TryAddWithoutValidation(PublicStoreProxyProof.OriginHeader, PublicHost);
        request.Headers.TryAddWithoutValidation(PublicStoreProxyProof.KeyHeader, StoreKey);
        request.Headers.TryAddWithoutValidation(PublicStoreProxyProof.AtHeader, proof.At);
        request.Headers.TryAddWithoutValidation(PublicStoreProxyProof.NonceHeader, proof.Nonce);
        request.Headers.TryAddWithoutValidation(PublicStoreProxyProof.ProofHeader, proof.Proof);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            requestTimeout.Token);
        var payload = await response.Content.ReadAsStringAsync(requestTimeout.Token);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (response.StatusCode == HttpStatusCode.OK)
        {
            if (!root.GetProperty("success").GetBoolean() ||
                !root.GetProperty("data").GetProperty("verificada").GetBoolean() ||
                root.GetProperty("data").GetProperty("empresaId").GetInt32() != expectedCompany)
                throw new InvalidOperationException("Response did not resolve the expected DEV company.");
        }
        else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            if (root.GetProperty("code").GetString() != "TENANT_DATA_ISOLATION_REQUIRED")
                throw new InvalidOperationException("Response does not carry the isolation rejection.");
        }
        return response.StatusCode;
    }

    private static async Task<int> CountDigestAsync(AppDbContext db, string proofHex,
        CancellationToken token)
    {
        // The ADO.NET parameter never goes to the logs. Exact digest lookup
        // proves the ledger accepted it only once, independent of other requests.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        await db.Database.OpenConnectionAsync(timeout.Token);
        try
        {
            using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM PublicStoreProofReplays WHERE ProofDigest=UNHEX(@digest)";
            cmd.CommandTimeout = 10;
            var parameter = cmd.CreateParameter();
            parameter.ParameterName = "@digest";
            parameter.Value = proofHex;
            cmd.Parameters.Add(parameter);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(timeout.Token),
                CultureInfo.InvariantCulture);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
