using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Interfaces;

public interface IImageStorageService
{
    /// Sube la imagen a Cloudinary y retorna (Url, PublicId).
    /// Contrato legacy: los nuevos consumidores tenant-owned deben usar el overload
    /// que exige <see cref="StorageTenantContext"/>.
    Task<(string Url, string PublicId)> UploadAsync(IFormFile file);

    /// Elimina la imagen de Cloudinary usando su PublicId.
    /// Contrato legacy: los nuevos consumidores tenant-owned deben usar el overload
    /// que exige <see cref="StorageTenantContext"/>.
    Task DeleteAsync(string publicId);

    /// Descarga una imagen cuyo ownership ya fue verificado por el llamador
    /// contra una entidad tenant-scoped persistida. La infraestructura debe
    /// aceptar únicamente locators del origen/cuenta/folder administrados,
    /// incluyendo imágenes válidas creadas antes del prefijo por Empresa.
    /// Devuelve null si el recurso ya no existe o no puede obtenerse.
    Task<(Stream Contenido, string ContentType)?> DownloadAsync(string url);

    /// <summary>
    /// Contrato tenant-aware para uploads de recursos propiedad de una Empresa.
    /// La implementación de infraestructura debe sobrescribirlo antes de que un
    /// consumidor lo use. El default falla cerrado para impedir fallback silencioso
    /// hacia el contrato legacy sin tenant.
    /// </summary>
    Task<(string Url, string PublicId)> UploadAsync(
        StorageTenantContext tenant,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(file);

        return Task.FromException<(string Url, string PublicId)>(
            new NotSupportedException(
                "El storage de imágenes debe implementar explícitamente el contrato tenant-aware antes de usarse."));
    }

    /// <summary>
    /// Contrato tenant-aware de borrado. Nunca debe autorizarse únicamente por
    /// publicId/ruta proporcionados por el cliente.
    /// </summary>
    Task DeleteAsync(
        StorageTenantContext tenant,
        string publicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("El publicId es obligatorio.", nameof(publicId));

        return Task.FromException(
            new NotSupportedException(
                "El storage de imágenes debe implementar explícitamente el borrado tenant-aware antes de usarse."));
    }

    /// <summary>
    /// Contrato tenant-aware de descarga para recursos privados/tenant-owned.
    /// El default falla cerrado hasta que infraestructura valide ownership y scope.
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
                "El storage de imágenes debe implementar explícitamente la descarga tenant-aware antes de usarse."));
    }
}
