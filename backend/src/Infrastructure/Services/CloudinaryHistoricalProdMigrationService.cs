using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryApp.Infrastructure.Services;

/// <summary>
/// Migración histórica fail-closed e idempotente hacia Cloudinary PROD.
/// Copia activos desde el cloud legacy autorizado y nunca elimina el activo origen.
/// </summary>
public sealed class CloudinaryHistoricalProdMigrationService
{
    private const string ExpectedDatabase = "solqaryn_prod";
    private const string ExpectedTargetCloud = "riyrzmob";
    private const string ExpectedEnvironmentPrefix = "solqaryn_prod";
    private const string SourceCloud = "vyijnqzq";
    private const long MaxAssetBytes = 20L * 1024L * 1024L;

    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CloudinaryHistoricalProdMigrationService> _logger;
    private readonly Cloudinary _cloudinary;

    public CloudinaryHistoricalProdMigrationService(
        AppDbContext db,
        IConfiguration configuration,
        ILogger<CloudinaryHistoricalProdMigrationService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;

        var cloudName = configuration["Cloudinary:CloudName"]?.Trim();
        var apiKey = configuration["Cloudinary:ApiKey"]?.Trim();
        var apiSecret = configuration["Cloudinary:ApiSecret"]?.Trim();

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException("Cloudinary PROD no está configurado para ejecutar la migración legacy.");
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
    }

    public async Task<CloudinaryHistoricalProdMigrationResult> MigrateAsync(
        CancellationToken cancellationToken = default)
    {
        ExigirScopeProdCanonico();

        var expectedRows = _configuration.GetValue<int>("CloudinaryHistoricalProdMigration:ExpectedLegacyRows");
        if (expectedRows <= 0)
            throw new InvalidOperationException("CloudinaryHistoricalProdMigration:ExpectedLegacyRows debe ser positivo.");

        var activeCompanyIds = await _db.Set<Empresa>()
            .AsNoTracking()
            .Where(x => x.Activa)
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (activeCompanyIds.Count != 1 || activeCompanyIds[0] <= 0)
        {
            throw new InvalidOperationException(
                $"La migración Cloudinary PROD exige exactamente una Empresa activa; observadas={activeCompanyIds.Count}.");
        }

        var empresaId = activeCompanyIds[0];

        var productImages = await _db.ProductoImagenes
            .Where(x =>
                x.Url.Contains($"res.cloudinary.com/{SourceCloud}/") ||
                x.PublicId.StartsWith("desarrollo/") ||
                x.PublicId.StartsWith("varistorehn_desarrollo/"))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var purchaseDocuments = await _db.CompraDocumentos
            .IgnoreQueryFilters()
            .Where(x =>
                x.Url.Contains($"res.cloudinary.com/{SourceCloud}/") ||
                x.PublicId.StartsWith("desarrollo/") ||
                x.PublicId.StartsWith("varistorehn_desarrollo/"))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var profileUsers = await _db.Usuarios
            .Where(x =>
                (x.FotoPerfilUrl != null && x.FotoPerfilUrl.Contains($"res.cloudinary.com/{SourceCloud}/")) ||
                (x.FotoPerfilPublicId != null &&
                    (x.FotoPerfilPublicId.StartsWith("desarrollo/") ||
                     x.FotoPerfilPublicId.StartsWith("varistorehn_desarrollo/"))))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var sourceRows = productImages.Count + purchaseDocuments.Count + profileUsers.Count;
        if (sourceRows != expectedRows)
        {
            throw new InvalidOperationException(
                $"Conteo Cloudinary legacy cambió desde el inventario certificado. Esperado={expectedRows} actual={sourceRows}.");
        }

        var productFolder = $"{ExpectedEnvironmentPrefix}/inventoryapp/productos/empresas/{empresaId}";
        var purchaseFolder = $"{ExpectedEnvironmentPrefix}/inventoryapp/compras/empresas/{empresaId}";
        var profileFolder = $"{ExpectedEnvironmentPrefix}/solqaryn/perfiles";

        var stagedProducts = new List<(ProductoImagen Entity, string Url, string PublicId)>();
        var stagedPurchases = new List<(CompraDocumento Entity, string Url, string PublicId)>();
        var stagedProfiles = new List<(Usuario Entity, string Url, string PublicId)>();

        foreach (var image in productImages)
        {
            var migrated = await CopyImageAsync(
                image.Url,
                productFolder,
                $"legacy-product-image-{image.Id}",
                cancellationToken);
            stagedProducts.Add((image, migrated.Url, migrated.PublicId));
        }

        foreach (var document in purchaseDocuments)
        {
            try
            {
                var migrated = string.Equals(document.ResourceType, "raw", StringComparison.OrdinalIgnoreCase)
                    ? await CopyRawAsync(
                        document.Url,
                        purchaseFolder,
                        $"legacy-purchase-document-{document.Id}{ExtensionFromUrl(document.Url)}",
                        cancellationToken)
                    : await CopyImageAsync(
                        document.Url,
                        purchaseFolder,
                        $"legacy-purchase-document-{document.Id}",
                        cancellationToken);
                stagedPurchases.Add((document, migrated.Url, migrated.PublicId));
            }
            catch (HttpRequestException ex) when (
                document.Eliminado &&
                ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "CLOUDINARY_HISTORICAL_PROD_MIGRATION TOMBSTONE missing_deleted_purchase_document id={DocumentId}",
                    document.Id);
                stagedPurchases.Add((document, string.Empty, string.Empty));
            }
        }

        foreach (var user in profileUsers)
        {
            var sourceUrl = user.FotoPerfilUrl
                ?? throw new InvalidOperationException($"Usuario {user.Id} tiene PublicId legacy sin URL.");
            var migrated = await CopyImageAsync(
                sourceUrl,
                profileFolder,
                $"legacy-profile-user-{user.Id}",
                cancellationToken);
            stagedProfiles.Add((user, migrated.Url, migrated.PublicId));
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var item in stagedProducts)
            {
                item.Entity.Url = item.Url;
                item.Entity.PublicId = item.PublicId;
            }

            foreach (var item in stagedPurchases)
            {
                item.Entity.Url = item.Url;
                item.Entity.PublicId = item.PublicId;
            }

            foreach (var item in stagedProfiles)
            {
                item.Entity.FotoPerfilUrl = item.Url;
                item.Entity.FotoPerfilPublicId = item.PublicId;
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        _db.ChangeTracker.Clear();

        var remaining = await CountLegacyRowsAsync(cancellationToken);
        if (remaining != 0)
        {
            throw new InvalidOperationException(
                $"Persisten referencias Cloudinary legacy después de la migración. Remaining={remaining}.");
        }

        _logger.LogInformation(
            "CLOUDINARY_HISTORICAL_PROD_MIGRATION SUCCESS source_rows={SourceRows} migrated_rows={MigratedRows} remaining=0 empresa={EmpresaId}",
            sourceRows,
            sourceRows,
            empresaId);

        return new CloudinaryHistoricalProdMigrationResult(
            "SUCCESS",
            sourceRows,
            sourceRows,
            remaining);
    }

    private void ExigirScopeProdCanonico()
    {
        var dbName = _db.Database.GetDbConnection().Database;
        var cloudName = _configuration["Cloudinary:CloudName"]?.Trim();
        var prefix = _configuration["Cloudinary:EnvironmentPrefix"]?.Trim();

        if (!string.Equals(dbName, ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cloudinary legacy migration bloqueada fuera de {ExpectedDatabase}.");

        if (!string.Equals(cloudName, ExpectedTargetCloud, StringComparison.Ordinal))
            throw new InvalidOperationException("Cloudinary legacy migration bloqueada: cloud destino no canónico.");

        if (!string.Equals(prefix, ExpectedEnvironmentPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Cloudinary legacy migration bloqueada: prefijo PROD no canónico.");
    }

    private async Task<int> CountLegacyRowsAsync(CancellationToken cancellationToken)
    {
        var products = await _db.ProductoImagenes
            .AsNoTracking()
            .CountAsync(x =>
                x.Url.Contains($"res.cloudinary.com/{SourceCloud}/") ||
                x.PublicId.StartsWith("desarrollo/") ||
                x.PublicId.StartsWith("varistorehn_desarrollo/"),
                cancellationToken);

        var purchases = await _db.CompraDocumentos
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(x =>
                x.Url.Contains($"res.cloudinary.com/{SourceCloud}/") ||
                x.PublicId.StartsWith("desarrollo/") ||
                x.PublicId.StartsWith("varistorehn_desarrollo/"),
                cancellationToken);

        var profiles = await _db.Usuarios
            .AsNoTracking()
            .CountAsync(x =>
                (x.FotoPerfilUrl != null && x.FotoPerfilUrl.Contains($"res.cloudinary.com/{SourceCloud}/")) ||
                (x.FotoPerfilPublicId != null &&
                    (x.FotoPerfilPublicId.StartsWith("desarrollo/") ||
                     x.FotoPerfilPublicId.StartsWith("varistorehn_desarrollo/"))),
                cancellationToken);

        return products + purchases + profiles;
    }

    private async Task<(string Url, string PublicId)> CopyImageAsync(
        string sourceUrl,
        string folder,
        string deterministicId,
        CancellationToken cancellationToken)
    {
        await using var asset = await DownloadLegacyAsync(sourceUrl, cancellationToken);
        var parameters = new ImageUploadParams
        {
            File = new FileDescription(asset.FileName, asset.Content),
            Folder = folder,
            PublicId = deterministicId,
            UseFilename = false,
            UniqueFilename = false,
            Overwrite = true
        };

        var result = await _cloudinary.UploadAsync(parameters);
        cancellationToken.ThrowIfCancellationRequested();

        if (result.Error is not null || result.SecureUrl is null || string.IsNullOrWhiteSpace(result.PublicId))
            throw new InvalidOperationException("Cloudinary rechazó una imagen durante la migración PROD.");

        ExigirDestinoCanonico(result.SecureUrl.ToString(), result.PublicId, folder);
        return (result.SecureUrl.ToString(), result.PublicId);
    }

    private async Task<(string Url, string PublicId)> CopyRawAsync(
        string sourceUrl,
        string folder,
        string deterministicId,
        CancellationToken cancellationToken)
    {
        await using var asset = await DownloadLegacyAsync(sourceUrl, cancellationToken);
        var parameters = new RawUploadParams
        {
            File = new FileDescription(asset.FileName, asset.Content),
            Folder = folder,
            PublicId = deterministicId,
            UseFilename = false,
            UniqueFilename = false,
            Overwrite = true
        };

        var result = await _cloudinary.UploadAsync(parameters);
        cancellationToken.ThrowIfCancellationRequested();

        if (result.Error is not null || result.SecureUrl is null || string.IsNullOrWhiteSpace(result.PublicId))
            throw new InvalidOperationException("Cloudinary rechazó un documento durante la migración PROD.");

        ExigirDestinoCanonico(result.SecureUrl.ToString(), result.PublicId, folder);
        return (result.SecureUrl.ToString(), result.PublicId);
    }

    private static void ExigirDestinoCanonico(string url, string publicId, string folder)
    {
        if (!publicId.StartsWith($"{folder}/", StringComparison.Ordinal))
            throw new InvalidOperationException("Cloudinary devolvió un PublicId fuera del prefijo PROD esperado.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith($"/{ExpectedTargetCloud}/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cloudinary devolvió una URL fuera del cloud destino esperado.");
        }
    }

    private static async Task<DownloadedLegacyAsset> DownloadLegacyAsync(
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException("Locator Cloudinary legacy inválido.");
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 4 ||
            !string.Equals(Uri.UnescapeDataString(segments[0]), SourceCloud, StringComparison.Ordinal) ||
            (segments[1] != "image" && segments[1] != "raw") ||
            segments[2] != "upload")
        {
            throw new InvalidOperationException("Locator no pertenece al cloud legacy autorizado.");
        }

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(45) };
        using var response = await client.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is long contentLength &&
            contentLength > MaxAssetBytes)
        {
            throw new InvalidOperationException("Asset legacy excede el máximo permitido para migración.");
        }

        await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
        var memory = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await remote.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;
            total += read;
            if (total > MaxAssetBytes)
            {
                await memory.DisposeAsync();
                throw new InvalidOperationException("Asset legacy excede el máximo permitido para migración.");
            }
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        memory.Position = 0;
        var fileName = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "legacy-asset.bin";

        return new DownloadedLegacyAsset(fileName, memory);
    }

    private static string ExtensionFromUrl(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            return string.Empty;
        var extension = Path.GetExtension(uri.AbsolutePath);
        return extension.Length is > 0 and <= 12 ? extension.ToLowerInvariant() : string.Empty;
    }

    private sealed class DownloadedLegacyAsset : IAsyncDisposable
    {
        public DownloadedLegacyAsset(string fileName, MemoryStream content)
        {
            FileName = fileName;
            Content = content;
        }

        public string FileName { get; }
        public MemoryStream Content { get; }

        public ValueTask DisposeAsync() => Content.DisposeAsync();
    }
}

public sealed record CloudinaryHistoricalProdMigrationResult(
    string Status,
    int SourceRows,
    int MigratedRows,
    int RemainingLegacyRows);
