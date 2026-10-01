using System.Security.Cryptography;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Infrastructure.Services;

public class CloudinaryCompraDocumentoStorageService : ICompraDocumentoStorageService
{
    private const string BaseFolder = "solqaryn/compras";
    private const long MaxDownloadBytes = 10 * 1024 * 1024;
    private const string TenantAuditMarker = "TENANT_STORAGE_AUDIT";
    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;
    private readonly string _folder;
    private readonly string? _environmentPrefix;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUsuarioScopeService? _usuarioScopeService;
    private readonly ILogger<CloudinaryCompraDocumentoStorageService>? _logger;

    public CloudinaryCompraDocumentoStorageService(
        IConfiguration configuration,
        IHttpContextAccessor? httpContextAccessor = null,
        IUsuarioScopeService? usuarioScopeService = null,
        ILogger<CloudinaryCompraDocumentoStorageService>? logger = null)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret) ||
            cloudName == "CHANGE_ME" ||
            apiKey == "CHANGE_ME" ||
            apiSecret == "CHANGE_ME")
        {
            throw new BusinessRuleException(
                "Cloudinary no está configurado para almacenar comprobantes de compras.");
        }

        _cloudName = cloudName.Trim();
        _cloudinary = new Cloudinary(new Account(_cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
        _folder = CloudinaryFolderResolver.Resolve(configuration, BaseFolder);
        _environmentPrefix = CloudinaryFolderResolver.GetEnvironmentPrefix(configuration);
        _httpContextAccessor = httpContextAccessor;
        _usuarioScopeService = usuarioScopeService;
        _logger = logger;
    }

    public async Task<DocumentoAlmacenado> UploadAsync(IFormFile archivo)
    {
        var tenant = await ResolverTenantActualAsync();
        return await UploadAsync(tenant, archivo, RequestAborted);
    }

    public async Task<DocumentoAlmacenado> UploadAsync(
        StorageTenantContext tenant,
        IFormFile archivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(archivo);
        cancellationToken.ThrowIfCancellationRequested();
        RegistrarPermitido(tenant, "upload", "TENANT_SCOPE_VERIFIED");
        return await UploadInternalAsync(archivo, TenantFolder(tenant), cancellationToken);
    }

    private async Task<DocumentoAlmacenado> UploadInternalAsync(
        IFormFile archivo,
        string folder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var stream = archivo.OpenReadStream();
            var esPdf = string.Equals(archivo.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

            if (esPdf)
            {
                var parametros = new RawUploadParams
                {
                    File = new FileDescription(archivo.FileName, stream),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true
                };

                var resultado = await _cloudinary.UploadAsync(parametros);
                cancellationToken.ThrowIfCancellationRequested();
                if (resultado.Error is not null)
                    throw new BusinessRuleException("Cloudinary rechazó el comprobante PDF.");

                return new DocumentoAlmacenado(
                    resultado.SecureUrl.ToString(),
                    resultado.PublicId,
                    "raw",
                    archivo.ContentType,
                    archivo.Length);
            }

            var parametrosImagen = new ImageUploadParams
            {
                File = new FileDescription(archivo.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true
            };

            var resultadoImagen = await _cloudinary.UploadAsync(parametrosImagen);
            cancellationToken.ThrowIfCancellationRequested();
            if (resultadoImagen.Error is not null)
                throw new BusinessRuleException("Cloudinary rechazó la imagen del comprobante.");

            return new DocumentoAlmacenado(
                resultadoImagen.SecureUrl.ToString(),
                resultadoImagen.PublicId,
                "image",
                archivo.ContentType,
                archivo.Length);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch
        {
            throw new BusinessRuleException(
                "No se pudo almacenar el comprobante en Cloudinary. Intenta nuevamente.");
        }
    }

    public async Task DeleteAsync(string publicId, string resourceType)
    {
        if (!CloudinaryFolderResolver.CanDelete(_environmentPrefix, publicId))
        {
            throw new BusinessRuleException(
                "El entorno de dev no puede eliminar un comprobante que pertenece a Producción.");
        }

        var tenant = await ResolverTenantActualAsync();
        await DeleteAsync(tenant, publicId, resourceType, RequestAborted);
    }

    public async Task DeleteAsync(
        StorageTenantContext tenant,
        string publicId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("El publicId es obligatorio.", nameof(publicId));
        if (string.IsNullOrWhiteSpace(resourceType))
            throw new ArgumentException("El resourceType es obligatorio.", nameof(resourceType));

        ExigirLocatorTenant(tenant, publicId, esUrl: false, operation: "delete");
        cancellationToken.ThrowIfCancellationRequested();
        RegistrarPermitido(tenant, "delete", "TENANT_LOCATOR_VERIFIED");
        await DeleteInternalAsync(publicId, resourceType);
    }

    private async Task DeleteInternalAsync(string publicId, string resourceType)
    {
        if (!CloudinaryFolderResolver.CanDelete(_environmentPrefix, publicId))
        {
            throw new BusinessRuleException(
                "El entorno de dev no puede eliminar un comprobante que pertenece a Producción.");
        }

        var parametros = new DeletionParams(publicId)
        {
            ResourceType = string.Equals(resourceType, "raw", StringComparison.OrdinalIgnoreCase)
                ? ResourceType.Raw
                : ResourceType.Image,
            Invalidate = true
        };

        var resultado = await _cloudinary.DestroyAsync(parametros);
        if (resultado.Error is not null)
            throw new BusinessRuleException("No se pudo retirar el comprobante de Cloudinary.");
    }

    public async Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url)
    {
        var tenant = await ResolverTenantActualAsync();
        return await DownloadAsync(tenant, url, RequestAborted);
    }

    public Task<(Stream Contenido, string ContentType)?> DownloadAsync(
        StorageTenantContext tenant,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL de almacenamiento es obligatoria.", nameof(url));

        ExigirLocatorTenant(tenant, url, esUrl: true, operation: "download");
        cancellationToken.ThrowIfCancellationRequested();
        RegistrarPermitido(tenant, "download", "TENANT_LOCATOR_VERIFIED");
        return DownloadInternalAsync(url, cancellationToken);
    }

    private async Task<(Stream Contenido, string ContentType)?> DownloadInternalAsync(
        string url,
        CancellationToken cancellationToken)
    {
        if (!EsUrlCloudinaryPermitida(url))
            return null;

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        try
        {
            using var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            if (response.Content.Headers.ContentLength is long contentLength &&
                contentLength > MaxDownloadBytes)
            {
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";
            await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
            var contenido = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                var read = await remote.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                    break;

                total += read;
                if (total > MaxDownloadBytes)
                {
                    await contenido.DisposeAsync();
                    return null;
                }

                await contenido.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            contenido.Position = 0;
            return (contenido, contentType);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private CancellationToken RequestAborted =>
        _httpContextAccessor?.HttpContext?.RequestAborted ?? default;

    private Task<StorageTenantContext> ResolverTenantActualAsync() =>
        StorageTenantContextResolver.ResolverRequeridoAsync(
            _httpContextAccessor,
            _usuarioScopeService,
            RequestAborted);

    private string TenantFolder(StorageTenantContext tenant) =>
        $"{_folder.TrimEnd('/')}/{tenant.TenantPrefix}";

    private void ExigirLocatorTenant(
        StorageTenantContext tenant,
        string locator,
        bool esUrl,
        string operation)
    {
        var tenantFolder = TenantFolder(tenant).Trim('/');
        if (!esUrl)
        {
            var normalizado = locator.Trim().Trim('/');
            if (!normalizado.StartsWith($"{tenantFolder}/", StringComparison.Ordinal))
            {
                Denegar(
                    tenant,
                    operation,
                    "TENANT_LOCATOR_MISMATCH",
                    "El comprobante solicitado no pertenece al contexto tenant verificado.");
            }

            return;
        }

        if (!EsUrlCloudinaryPermitida(locator))
        {
            Denegar(
                tenant,
                operation,
                "LOCATOR_ORIGIN_INVALID",
                "La URL del comprobante no pertenece al origen Cloudinary configurado.");
        }

        var uri = new Uri(locator, UriKind.Absolute);
        var marker = $"/{tenantFolder}/";
        if (!uri.AbsolutePath.Contains(marker, StringComparison.Ordinal))
        {
            Denegar(
                tenant,
                operation,
                "TENANT_LOCATOR_MISMATCH",
                "El comprobante solicitado no pertenece al contexto tenant verificado.");
        }
    }

    private bool EsUrlCloudinaryPermitida(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Length >= 3 &&
               string.Equals(Uri.UnescapeDataString(segments[0]), _cloudName, StringComparison.Ordinal) &&
               (string.Equals(segments[1], "image", StringComparison.Ordinal) ||
                string.Equals(segments[1], "raw", StringComparison.Ordinal)) &&
               string.Equals(segments[2], "upload", StringComparison.Ordinal);
    }

    private void RegistrarPermitido(
        StorageTenantContext tenant,
        string operation,
        string reasonCode)
    {
        _logger?.LogInformation(
            "{AuditMarker} operation={Operation} outcome={Outcome} reason={ReasonCode} tenant_ref={TenantRef}",
            TenantAuditMarker,
            operation,
            "SCOPE_ALLOW",
            reasonCode,
            TenantReference(tenant));
    }

    private void RegistrarDenegado(
        StorageTenantContext tenant,
        string operation,
        string reasonCode)
    {
        _logger?.LogWarning(
            "{AuditMarker} operation={Operation} outcome={Outcome} reason={ReasonCode} tenant_ref={TenantRef}",
            TenantAuditMarker,
            operation,
            "DENY_SAFE",
            reasonCode,
            TenantReference(tenant));
    }

    private void Denegar(
        StorageTenantContext tenant,
        string operation,
        string reasonCode,
        string safeMessage)
    {
        RegistrarDenegado(tenant, operation, reasonCode);
        throw new BusinessRuleException(safeMessage);
    }

    private static string TenantReference(StorageTenantContext tenant)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"empresa:{tenant.EmpresaId}"));
        return Convert.ToHexString(bytes.AsSpan(0, 6));
    }
}
