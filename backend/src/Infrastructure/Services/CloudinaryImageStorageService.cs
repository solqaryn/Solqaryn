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

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _cloudName;
    private readonly string _folder;
    private readonly string? _environmentPrefix;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUsuarioScopeService? _usuarioScopeService;
    private readonly ILogger<CloudinaryImageStorageService>? _logger;
    private const string BaseFolder = "solqaryn/productos";
    private const string TenantAuditMarker = "TENANT_STORAGE_AUDIT";

    public CloudinaryImageStorageService(
        IConfiguration configuration,
        IHttpContextAccessor? httpContextAccessor = null,
        IUsuarioScopeService? usuarioScopeService = null,
        ILogger<CloudinaryImageStorageService>? logger = null)
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
                "Cloudinary no está configurado. Revisa Cloudinary:CloudName, Cloudinary:ApiKey y Cloudinary:ApiSecret.");
        }

        _cloudName = cloudName.Trim();
        var account = new Account(_cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
        _folder = CloudinaryFolderResolver.Resolve(configuration, BaseFolder);
        _environmentPrefix = CloudinaryFolderResolver.GetEnvironmentPrefix(configuration);
        _httpContextAccessor = httpContextAccessor;
        _usuarioScopeService = usuarioScopeService;
        _logger = logger;
    }

    public async Task<(string Url, string PublicId)> UploadAsync(IFormFile file)
    {
        var tenant = await ResolverTenantActualAsync();
        return await UploadAsync(tenant, file, RequestAborted);
    }

    public async Task<(string Url, string PublicId)> UploadAsync(
        StorageTenantContext tenant,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(file);
        cancellationToken.ThrowIfCancellationRequested();
        RegistrarPermitido(tenant, "upload", "TENANT_SCOPE_VERIFIED");
        return await UploadInternalAsync(file, TenantFolder(tenant), cancellationToken);
    }

    private async Task<(string Url, string PublicId)> UploadInternalAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var segura = await ImagenUploadSecurity.ProcesarAsync(file);
            cancellationToken.ThrowIfCancellationRequested();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(segura.NombreArchivo, segura.Contenido),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation().Width(800).Height(800).Crop("limit").Quality("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error is not null || result.SecureUrl is null || string.IsNullOrWhiteSpace(result.PublicId))
                throw new BusinessRuleException("No se pudo guardar la imagen del producto.");

            return (result.SecureUrl.ToString(), result.PublicId);
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
            // No se devuelve al cliente el mensaje técnico del proveedor externo,
            // evitando filtrar detalles de configuración o infraestructura.
            throw new BusinessRuleException(
                "No se pudo guardar la imagen del producto. Intenta nuevamente.");
        }
    }

    public async Task DeleteAsync(string publicId)
    {
        if (!CloudinaryFolderResolver.CanDelete(_environmentPrefix, publicId))
        {
            throw new BusinessRuleException(
                "El entorno de dev no puede eliminar una imagen que pertenece a Producción.");
        }

        var tenant = await ResolverTenantActualAsync();
        await DeleteAsync(tenant, publicId, RequestAborted);
    }

    public async Task DeleteAsync(
        StorageTenantContext tenant,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("El publicId es obligatorio.", nameof(publicId));

        ExigirLocatorTenant(tenant, publicId, esUrl: false, operation: "delete");
        cancellationToken.ThrowIfCancellationRequested();
        RegistrarPermitido(tenant, "delete", "TENANT_LOCATOR_VERIFIED");
        await DeleteInternalAsync(publicId);
    }

    private async Task DeleteInternalAsync(string publicId)
    {
        if (!CloudinaryFolderResolver.CanDelete(_environmentPrefix, publicId))
        {
            throw new BusinessRuleException(
                "El entorno de dev no puede eliminar una imagen que pertenece a Producción.");
        }

        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    public Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL de almacenamiento es obligatoria.", nameof(url));

        // Este contrato se usa únicamente cuando el llamador ya comprobó ownership
        // contra una entidad cargada desde el repositorio tenant-scoped (por ejemplo,
        // Producto -> ProductoImagen). Permite imágenes válidas previas a la carpeta
        // /empresas/{id}/ sin aceptar orígenes, cuentas o folders arbitrarios.
        ExigirLocatorProductoAdministrado(url);
        return DownloadInternalAsync(url, RequestAborted);
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

    private static async Task<(Stream Contenido, string ContentType)?> DownloadInternalAsync(
        string url,
        CancellationToken cancellationToken)
    {
        // La respuesta de Cloudinary se materializa antes de liberar HttpClient/HttpResponseMessage.
        // Devolver directamente response.Content.ReadAsStreamAsync() dejaba al controlador con
        // un stream cuya conexión podía cerrarse al salir de este método.
        using var httpClient = new HttpClient();
        try
        {
            using var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var memory = new MemoryStream();
            await response.Content.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;
            return (memory, contentType);
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

    private void ExigirLocatorProductoAdministrado(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new BusinessRuleException(
                "La URL de la imagen no pertenece al almacenamiento administrado por SOLQARYN.");
        }

        var segments = uri!.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 4 ||
            !string.Equals(Uri.UnescapeDataString(segments[0]), _cloudName, StringComparison.Ordinal) ||
            !string.Equals(segments[1], "image", StringComparison.Ordinal) ||
            !string.Equals(segments[2], "upload", StringComparison.Ordinal))
        {
            throw new BusinessRuleException(
                "La URL de la imagen no pertenece a la cuenta Cloudinary configurada.");
        }

        var configuredFolder = _folder.Trim('/');
        var decodedPath = Uri.UnescapeDataString(uri.AbsolutePath);
        var marker = $"/{configuredFolder}/";
        if (!decodedPath.Contains(marker, StringComparison.Ordinal))
        {
            throw new BusinessRuleException(
                "La URL de la imagen no pertenece al folder administrado de productos.");
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
                    "El recurso solicitado no pertenece al contexto tenant verificado.");
            }

            return;
        }

        if (!Uri.TryCreate(locator, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            Denegar(
                tenant,
                operation,
                "LOCATOR_ORIGIN_INVALID",
                "La URL de almacenamiento no es un locator seguro para este tenant.");
        }

        var segments = uri!.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 4 ||
            !string.Equals(Uri.UnescapeDataString(segments[0]), _cloudName, StringComparison.Ordinal) ||
            !string.Equals(segments[1], "image", StringComparison.Ordinal) ||
            !string.Equals(segments[2], "upload", StringComparison.Ordinal))
        {
            Denegar(
                tenant,
                operation,
                "CLOUD_ACCOUNT_MISMATCH",
                "La URL de almacenamiento no pertenece al origen Cloudinary configurado.");
        }

        var marker = $"/{tenantFolder}/";
        if (!uri.AbsolutePath.Contains(marker, StringComparison.Ordinal))
        {
            Denegar(
                tenant,
                operation,
                "TENANT_LOCATOR_MISMATCH",
                "El recurso solicitado no pertenece al contexto tenant verificado.");
        }
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
