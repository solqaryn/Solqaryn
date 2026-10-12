using System.Security.Cryptography;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// One-time DEV-only normalization. Never runs without the explicit checked-in
/// Development toggle. Original Cloudinary assets are NEVER removed.
/// Database updates are compare-and-swap for each image after byte-for-byte
/// Cloudinary destination verification. Operational prerequisite: encrypted
/// Aiven DEV backup + independently verified disposable restore.
/// </summary>
public sealed class DevCloudinaryLegacyNormalizationHostedService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    IHostEnvironment hostEnvironment,
    ILogger<DevCloudinaryLegacyNormalizationHostedService> logger) : BackgroundService
{
    private const string AllowedHost = "solqaryn-mysql-solqaryn.h.aivencloud.com";
    private const string AllowedCloud = "riyrzmob";
    private const string AllowedPrefix = "solqaryn_dev";
    private const int ExpectedHistoricalImages = 11;
    private const int MaxImageBytes = 10 * 1024 * 1024;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await NormalizeAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Deployment shutting down; no unverified references are committed.
        }
        catch (Exception ex)
        {
            // Do not expose URLs, public IDs, tenants, binary data or credentials.
            logger.LogError("CLOUDINARY_DEV_NORMALIZATION_STOP reason={Reason}", ex.GetType().Name);
        }
    }

    private async Task NormalizeAsync(CancellationToken token)
    {
        if (!hostEnvironment.IsDevelopment() ||
            !string.Equals(Environment.GetEnvironmentVariable("RENDER"), "true", StringComparison.OrdinalIgnoreCase) ||
            !configuration.GetValue<bool>("Cloudinary:NormalizeDevLegacyImagesOnStartup") ||
            configuration["Cloudinary:CloudName"] != AllowedCloud ||
            configuration["Cloudinary:EnvironmentPrefix"] != AllowedPrefix)
            throw new InvalidOperationException("Cloudinary normalization restricted to explicitly enabled corporate DEV.");

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DEV connection is required.");
        var connection = new MySqlConnectionStringBuilder(connectionString);
        if (connection.Server != AllowedHost ||
            connection.Port != 14402 ||
            connection.Database != "solqaryn_dev" ||
            connection.UserID != "solqaryn_dev_user" ||
            !string.Equals(connection.SslMode.ToString(), "Required", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cloudinary normalization refused unsafe MySQL binding.");

        var key = configuration["Cloudinary:ApiKey"];
        var secret = configuration["Cloudinary:ApiSecret"];
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Cloudinary DEV credentials are unavailable.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var companies = await db.Empresas.AsNoTracking().Select(e => e.Id).ToListAsync(token);
        if (companies.Count != 1 || companies[0] <= 0)
            throw new InvalidOperationException("Tenant ownership is not uniquely provable.");

        var tenantId = companies[0];
        var images = await db.ProductoImagenes.AsNoTracking()
            .OrderBy(i => i.Id)
            .Select(i => new { i.Id, i.Url, i.PublicId })
            .ToListAsync(token);
        if (images.Count != ExpectedHistoricalImages)
            throw new InvalidOperationException("Historical image inventory changed; review authorization again.");

        var pending = new List<(int Id, string Url, string PublicId, string Extension)>();
        foreach (var item in images)
        {
            if (!DevCloudinaryLegacyLocator.TryValidate(
                    item.Url, item.PublicId, tenantId, out var extension, out var canonical))
                throw new InvalidOperationException("A DEV image locator did not pass strict ownership validation.");
            if (!canonical)
                pending.Add((item.Id, item.Url, item.PublicId, extension));
        }

        if (pending.Count == 0)
        {
            logger.LogInformation("CLOUDINARY_DEV_NORMALIZATION=PASS already_canonical=11 remaining=0");
            return;
        }

        // Fail closed on a fresh batch that is not the authorized historical set.
        if (pending.Count > ExpectedHistoricalImages)
            throw new InvalidOperationException("Historical image set exceeds authorization.");

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(45) };
        var cloudinary = new Cloudinary(new Account(AllowedCloud, key, secret));
        cloudinary.Api.Secure = true;

        // Every source must be reachable and pass a magic-byte check BEFORE any write.
        var verified = new List<(int Id, string Url, string PublicId, string Extension, byte[] Bytes, byte[] Hash)>();
        foreach (var item in pending)
        {
            var bytes = await ReadSafeImageAsync(http, item.Url, item.Extension, token);
            verified.Add((item.Id, item.Url, item.PublicId, item.Extension, bytes, SHA256.HashData(bytes)));
        }

        var changed = 0;
        foreach (var item in verified)
        {
            token.ThrowIfCancellationRequested();
            var folder = $"{AllowedPrefix}/solqaryn/productos/empresas/{tenantId}";
            var shortHash = Convert.ToHexString(item.Hash.AsSpan(0, 10)).ToLowerInvariant();
            var destinationName = $"normalizada-{item.Id}-{shortHash}";
            using var source = new MemoryStream(item.Bytes, writable: false);
            var upload = new ImageUploadParams
            {
                File = new FileDescription($"normalizada-{item.Id}{item.Extension}", source),
                Folder = folder,
                PublicId = destinationName,
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = true
            };
            var result = await cloudinary.UploadAsync(upload);
            if (result.Error is not null ||
                result.SecureUrl is null ||
                result.PublicId != $"{folder}/{destinationName}")
                throw new InvalidOperationException("Cloudinary DEV refused a canonical destination.");

            var nextUrl = result.SecureUrl.ToString();
            if (!DevCloudinaryLegacyLocator.TryValidate(
                    nextUrl, result.PublicId, tenantId, out var newExtension, out var canonical) ||
                !canonical || newExtension != item.Extension)
                throw new InvalidOperationException("Cloudinary DEV destination locator is not canonical.");

            var copied = await ReadSafeImageAsync(http, nextUrl, item.Extension, token);
            if (!SHA256.HashData(copied).AsSpan().SequenceEqual(item.Hash))
                throw new InvalidOperationException("Cloudinary copy differs from original bytes.");

            // Single-statement atomic compare-and-swap prevents overwriting any
            // concurrent replacement. Original media remains untouched.
            var rows = await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE ProductoImagenes SET Url={nextUrl}, PublicId={result.PublicId} WHERE Id={item.Id} AND Url={item.Url} AND PublicId={item.PublicId};",
                token);
            if (rows != 1)
                throw new InvalidOperationException("Image row changed during normalization.");

            changed++;
            logger.LogInformation("CLOUDINARY_DEV_NORMALIZATION_PROGRESS completed={Completed}", changed);
        }

        var after = await db.ProductoImagenes.AsNoTracking()
            .Select(i => new { i.Url, i.PublicId })
            .ToListAsync(token);
        var unresolved = after.Count(x => !DevCloudinaryLegacyLocator.TryValidate(
            x.Url, x.PublicId, tenantId, out _, out var canonical) || !canonical);

        if (unresolved != 0 || after.Count != ExpectedHistoricalImages)
            throw new InvalidOperationException("Cloudinary normalization readback is incomplete.");

        logger.LogInformation("CLOUDINARY_DEV_NORMALIZATION=PASS total=11 newly_normalized={Normalized} remaining=0 originals_deleted=0", changed);
    }

    private static async Task<byte[]> ReadSafeImageAsync(
        HttpClient http, string url, string extension, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode != System.Net.HttpStatusCode.OK ||
            response.Content.Headers.ContentLength > MaxImageBytes)
            throw new InvalidOperationException("Cloudinary source/destination is unavailable or oversized.");

        await using var input = await response.Content.ReadAsStreamAsync(token);
        using var destination = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer, token)) > 0)
        {
            if (destination.Length + read > MaxImageBytes)
                throw new InvalidOperationException("Cloudinary image exceeds configured size.");
            await destination.WriteAsync(buffer.AsMemory(0, read), token);
        }

        var bytes = destination.ToArray();
        if (bytes.Length < 12 || !DevCloudinaryLegacyLocator.MatchesImageSignature(bytes, extension))
            throw new InvalidOperationException("Cloudinary binary signature mismatch.");
        return bytes;
    }
}

public static class DevCloudinaryLegacyLocator
{
    private const string Cloud = "riyrzmob";
    private const string Env = "solqaryn_dev";

    public static bool TryValidate(
        string? url, string? publicId, int empresaId, out string extension, out bool canonical)
    {
        extension = string.Empty;
        canonical = false;
        if (empresaId <= 0 || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(publicId) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.Host != "res.cloudinary.com" ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath.Contains('%') || uri.AbsolutePath.Contains('\\'))
            return false;

        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 10 || parts[0] != Cloud || parts[1] != "image" ||
            parts[2] != "upload" || parts[3].Length < 2 || parts[3][0] != 'v' ||
            !parts[3].AsSpan(1).ToString().All(char.IsAsciiDigit) ||
            parts[4] != Env || parts[6] != "productos" || parts[7] != "empresas" ||
            parts[8] != empresaId.ToString(System.Globalization.CultureInfo.InvariantCulture))
            return false;

        var origin = parts[5];
        if (origin.Length is < 1 or > 40 ||
            !origin.All(ch => char.IsAsciiLetterOrDigit(ch) || ch == '_' || ch == '-'))
            return false;

        var filename = parts[9];
        var dot = filename.LastIndexOf('.');
        if (dot <= 0 || dot == filename.Length - 1)
            return false;
        extension = filename[dot..].ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return false;

        var stem = filename[..dot];
        if (string.IsNullOrWhiteSpace(stem) ||
            !stem.All(ch => char.IsAsciiLetterOrDigit(ch) || ch == '_' || ch == '-'))
            return false;

        var expectedId = $"{Env}/{origin}/productos/empresas/{empresaId}/{stem}";
        if (publicId != expectedId)
            return false;
        canonical = origin == "solqaryn";
        return true;
    }

    public static bool MatchesImageSignature(ReadOnlySpan<byte> image, string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => image.Length >= 3 && image[0] == 0xFF && image[1] == 0xD8 && image[2] == 0xFF,
            ".png" => image.Length >= 8 && image[..8].SequenceEqual(
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => image.Length >= 12 &&
                image[..4].SequenceEqual("RIFF"u8) &&
                image.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
