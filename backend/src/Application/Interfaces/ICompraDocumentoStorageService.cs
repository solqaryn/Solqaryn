using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Interfaces;

public record DocumentoAlmacenado(
    string Url,
    string PublicId,
    string ResourceType,
    string ContentType,
    long SizeBytes);

public interface ICompraDocumentoStorageService
{
    /// Contrato legacy. Los consumidores tenant-owned deben migrar al overload
    /// que exige <see cref="StorageTenantContext"/>.
    Task<DocumentoAlmacenado> UploadAsync(IFormFile archivo);

    /// Contrato legacy. Los consumidores tenant-owned deben migrar al overload
    /// que exige <see cref="StorageTenantContext"/>.
    Task DeleteAsync(string publicId, string resourceType);

    /// Contrato legacy. Los consumidores tenant-owned deben migrar al overload
    /// que exige <see cref="StorageTenantContext"/>.
    Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url);

    /// <summary>
    /// Upload tenant-aware de un documento privado. El default falla cerrado
    /// hasta que la implementación de infraestructura valide y materialice el
    /// scope tenant explícito.
    /// </summary>
    Task<DocumentoAlmacenado> UploadAsync(
        StorageTenantContext tenant,
        IFormFile archivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(archivo);

        return Task.FromException<DocumentoAlmacenado>(
            new NotSupportedException(
                "El storage de documentos de compra debe implementar explícitamente el contrato tenant-aware antes de usarse."));
    }

    /// <summary>
    /// Borrado tenant-aware de documentos privados. El publicId por sí solo no
    /// constituye autoridad de acceso.
    /// </summary>
    Task DeleteAsync(
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

        return Task.FromException(
            new NotSupportedException(
                "El storage de documentos de compra debe implementar explícitamente el borrado tenant-aware antes de usarse."));
    }

    /// <summary>
    /// Descarga tenant-aware de documentos privados. El default falla cerrado
    /// hasta que infraestructura valide ownership y scope contra el tenant.
    /// </summary>
    Task<(Stream Contenido, string ContentType)?> DownloadAsync(
        StorageTenantContext tenant,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL de almacenamiento es obligatoria.", nameof(url));

        return Task.FromException<(Stream Contenido, string ContentType)?>(
            new NotSupportedException(
                "El storage de documentos de compra debe implementar explícitamente la descarga tenant-aware antes de usarse."));
    }
}
