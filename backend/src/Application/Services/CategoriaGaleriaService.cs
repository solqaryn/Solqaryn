using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class CategoriaGaleriaService(
    ICategoriaRepository categorias,
    ICategoriaGaleriaRepository galerias,
    IImageStorageService storage,
    IAuditoriaService auditoria,
    ILogger<CategoriaGaleriaService> logger) : ICategoriaGaleriaService
{
    private const int MaxImagenes = ImagenValidationHelper.MaxImagenes;
    private const long MaxTotalBytes = 4L * 1024 * 1024;
    private sealed record ImagenPersistida(string Id, string Url, string PublicId);

    public async Task<CategoriaGaleriaDto?> ObtenerAsync(StorageTenantContext tenant, int categoriaId, CancellationToken cancellationToken)
    {
        if (await categorias.GetByIdAsync(categoriaId) is null) return null;
        var galeria = await galerias.GetAsync(tenant.EmpresaId, categoriaId, cancellationToken);
        return ToDto(galeria?.Version ?? string.Empty, Leer(galeria));
    }

    public async Task<CategoriaGaleriaDto?> GuardarAsync(StorageTenantContext tenant, int categoriaId, ActualizarCategoriaGaleriaDto dto, CancellationToken cancellationToken)
    {
        if (await categorias.GetByIdAsync(categoriaId) is null) return null;
        if (dto.Orden.Count > MaxImagenes || dto.Archivos.Count > MaxImagenes)
            throw new BusinessRuleException("Cada categoría permite un máximo de cinco imágenes.");
        if (dto.Archivos.Any(a => !ImagenValidationHelper.EsImagenValida(a)) || dto.Archivos.Sum(a => a.Length) > MaxTotalBytes)
            throw new BusinessRuleException("Utiliza JPG, PNG o WebP válidos; el envío optimizado no puede superar 4 MB.");

        var galeria = await galerias.GetAsync(tenant.EmpresaId, categoriaId, cancellationToken);
        var nueva = galeria is null;
        if (!string.Equals(dto.Version ?? string.Empty, galeria?.Version ?? string.Empty, StringComparison.Ordinal))
            throw new BusinessRuleException("La galería cambió en otra sesión. Vuelve a cargar la categoría antes de guardar.");
        var actuales = Leer(galeria);
        var porId = actuales.ToDictionary(i => i.Id, StringComparer.Ordinal);
        var vistas = new HashSet<string>(StringComparer.Ordinal);
        var indices = new HashSet<int>();
        foreach (var referencia in dto.Orden)
        {
            if (string.IsNullOrWhiteSpace(referencia) || !vistas.Add(referencia))
                throw new BusinessRuleException("El orden de las imágenes no es válido.");
            if (referencia.StartsWith("e:", StringComparison.Ordinal) && porId.ContainsKey(referencia[2..]))
                continue;
            if (referencia.StartsWith("n:", StringComparison.Ordinal)
                && int.TryParse(referencia[2..], NumberStyles.None, CultureInfo.InvariantCulture, out var indice)
                && indice >= 0 && indice < dto.Archivos.Count
                && referencia == "n:" + indice.ToString(CultureInfo.InvariantCulture) && indices.Add(indice))
                continue;
            throw new BusinessRuleException("Una imagen no pertenece a esta galería o su orden es inválido.");
        }
        if (indices.Count != dto.Archivos.Count)
            throw new BusinessRuleException("Todas las imágenes nuevas deben formar parte de la galería.");

        var subidas = new List<ImagenPersistida>();
        var resultado = new List<ImagenPersistida>();
        var persistida = false;
        try
        {
            foreach (var referencia in dto.Orden)
            {
                if (referencia.StartsWith("e:", StringComparison.Ordinal))
                {
                    resultado.Add(porId[referencia[2..]]);
                    continue;
                }
                var indice = int.Parse(referencia[2..], CultureInfo.InvariantCulture);
                var (url, publicId) = await storage.UploadAsync(tenant, dto.Archivos[indice], cancellationToken);
                var imagen = new ImagenPersistida(Guid.NewGuid().ToString("N"), url, publicId);
                subidas.Add(imagen);
                resultado.Add(imagen);
            }
            galeria ??= new CategoriaGaleria { EmpresaId = tenant.EmpresaId, CategoriaId = categoriaId };
            galeria.Version = Guid.NewGuid().ToString("N");
            galeria.ImagenesJson = JsonSerializer.Serialize(resultado);
            await galerias.SaveAsync(galeria, nueva, cancellationToken);
            persistida = true;
        }
        finally
        {
            if (!persistida)
                foreach (var imagen in subidas)
                    await LimpiarAsync(tenant, imagen.PublicId);
        }
        await auditoria.RegistrarAsync(ModuloSistema.Categorias, AccionPermiso.Editar,
            $"Galería de categoría {categoriaId} actualizada: {resultado.Count} imágenes.",
            categoriaId, entidad: "CategoriaGaleria", valoresAnteriores: new { Cantidad = actuales.Count },
            valoresNuevos: new { Cantidad = resultado.Count });
        var retenidas = resultado.Select(i => i.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var imagen in actuales.Where(i => !retenidas.Contains(i.Id)))
            await LimpiarAsync(tenant, imagen.PublicId);
        return ToDto(galeria!.Version, resultado);
    }

    public async Task<Dictionary<int, string[]>> ObtenerPublicasAsync(int empresaId, IReadOnlyCollection<int> categoriaIds, CancellationToken cancellationToken)
    {
        if (empresaId <= 0 || categoriaIds.Count == 0) return new();
        var filas = await galerias.GetByCategoriasAsync(empresaId, categoriaIds, cancellationToken);
        var resultado = new Dictionary<int, string[]>();
        foreach (var fila in filas.Where(f => f.EmpresaId == empresaId && categoriaIds.Contains(f.CategoriaId)))
        {
            try { resultado[fila.CategoriaId] = Leer(fila).Select(i => i.Url).ToArray(); }
            catch (BusinessRuleException)
            {
                logger.LogWarning("Galería pública inválida de categoría {CategoriaId}; no se publicarán sus imágenes.", fila.CategoriaId);
            }
        }
        return resultado;
    }

    private static List<ImagenPersistida> Leer(CategoriaGaleria? galeria)
    {
        if (galeria is null) return new();
        try
        {
            var imagenes = JsonSerializer.Deserialize<List<ImagenPersistida>>(galeria.ImagenesJson);
            if (imagenes is null || imagenes.Count > MaxImagenes
                || imagenes.Any(i => i is null || !Guid.TryParseExact(i.Id, "N", out _)
                    || !Uri.TryCreate(i.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
                    || string.IsNullOrWhiteSpace(i.PublicId))
                || imagenes.Select(i => i.Id).Distinct(StringComparer.Ordinal).Count() != imagenes.Count)
                throw new JsonException();
            return imagenes;
        }
        catch (JsonException)
        {
            throw new BusinessRuleException("La galería almacenada no es válida. No se modificaron sus imágenes.");
        }
    }

    private static CategoriaGaleriaDto ToDto(string version, IReadOnlyCollection<ImagenPersistida> imagenes) =>
        new(version, imagenes.Select(i => new CategoriaImagenDto(i.Id, i.Url)).ToArray());

    private async Task LimpiarAsync(StorageTenantContext tenant, string publicId)
    {
        try { await storage.DeleteAsync(tenant, publicId, CancellationToken.None); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo limpiar una imagen de categoría del tenant {EmpresaId}; requiere reconciliación de almacenamiento.", tenant.EmpresaId);
        }
    }
}
