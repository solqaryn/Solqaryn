using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Solqaryn.API.Middleware;

/// <summary>
/// Attestation v1 del proxy del proyecto Vercel autorizado.
/// Jamás usa X-Forwarded-Host, X-Empresa-Id, Origin o Referer como autoridad.
/// No concede permisos privados: sólo identifica la tienda pública para que
/// el resolver la contraste con la allowlist y MySQL compruebe su vigencia.
/// </summary>
public static class PublicStoreProxyProof
{
    public const string OriginHeader = "X-Solqaryn-Store-Origin";
    public const string KeyHeader = "X-Solqaryn-Store-Key";
    public const string AtHeader = "X-Solqaryn-Store-At";
    public const string ProofHeader = "X-Solqaryn-Store-Proof";
    private const long MaxClockSkewSeconds = 60;

    public static bool HasProofHeaders(HttpRequest request) =>
        request.Headers.ContainsKey(OriginHeader) ||
        request.Headers.ContainsKey(KeyHeader) ||
        request.Headers.ContainsKey(AtHeader) ||
        request.Headers.ContainsKey(ProofHeader);

    public static bool TryReadVerified(HttpRequest request, IConfiguration config,
        out HostString publicHost, out string? storeKey)
    {
        publicHost = default;
        storeKey = null;
        if (!request.Path.StartsWithSegments("/tienda", StringComparison.OrdinalIgnoreCase) ||
            !HasProofHeaders(request)) return false;

        // Rechaza cabeceras duplicadas/combinadas. Ningún fallback a Host Render.
        if (!Single(request, OriginHeader, out var host) ||
            !Single(request, KeyHeader, out var key) ||
            !Single(request, AtHeader, out var at) ||
            !Single(request, ProofHeader, out var proof))
            return false;

        var secret = config["PublicStore:ProxySigningKey"];
        if (string.IsNullOrEmpty(secret) || Encoding.UTF8.GetByteCount(secret) < 32 ||
            host.Length is < 4 or > 253 || host != host.ToLowerInvariant() ||
            host.Contains(':') || host.Contains('/') || host.Contains('\\') ||
            !host.Contains('.') || proof.Length != 64 ||
            proof.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')) ||
            !long.TryParse(at, NumberStyles.None, CultureInfo.InvariantCulture, out var timestamp))
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (timestamp < now - MaxClockSkewSeconds || timestamp > now + MaxClockSkewSeconds)
            return false;

        if (key != "-" && (key.Length is < 1 or > 48 ||
            key[0] is not (>= 'a' and <= 'z') ||
            key[^1] is not (>= 'a' and <= 'z' or >= '0' and <= '9') ||
            key.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))))
            return false;

        var canonical = $"v1\n{at}\n{request.Method.ToUpperInvariant()}\n{request.Path.Value}\n{host}\n{key}";
        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonical));
        Span<byte> provided = stackalloc byte[32];
        if (!Convert.TryFromHexString(proof, provided, out var written) || written != 32 ||
            !CryptographicOperations.FixedTimeEquals(provided, expected))
            return false;

        publicHost = new HostString(host);
        storeKey = key == "-" ? null : key;
        return true;
    }

    private static bool Single(HttpRequest request, string name, out string value)
    {
        value = string.Empty;
        if (!request.Headers.TryGetValue(name, out var values) || values.Count != 1 ||
            string.IsNullOrWhiteSpace(values[0]) || values[0]!.Length > 512)
            return false;
        value = values[0]!;
        return true;
    }
}
