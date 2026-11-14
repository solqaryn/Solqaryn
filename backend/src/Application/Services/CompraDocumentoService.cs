using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Services;

public class CompraDocumentoService : ICompraDocumentoService
{
    private const int MaxDocumentosPorCompra = 10;
    private const long MaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> ContentTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf"
    };

    private readonly ICompraRepository _compraRepository;
    private readonly ICompraDocumentoRepository _documentoRepository;
    private readonly ICompraDocumentoStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUsuarioScopeService? _usuarioScopeService;

    public CompraDocumentoService(
        ICompraRepository compraRepository,
        ICompraDocumentoRepository documentoRepository,
        ICompraDocumentoStorageService storage,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        IHttpContextAccessor? httpContextAccessor = null,
        IUsuarioScopeService? usuarioScopeService = null)
    {
        _compraRepository = compraRepository;
        _documentoRepository = documentoRepository;
        _storage = storage;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _httpContextAccessor = httpContextAccessor;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<List<CompraDocumentoDto>> GetByCompraAsync(int compraId)
    {
        _ = await ResolverStorageTenantAsync();
        await ObtenerCompraAutorizadaAsync(compraId);
        var documentos = await _documentoRepository.GetByCompraIdAsync(compraId);
        return documentos.Select(ToDto).ToList();
    }

    public async Task<CompraDocumentoDto> UploadAsync(int compraId, IFormFile archivo)
    {
        var tenant = await ResolverStorageTenantAsync();
        var cancellationToken = RequestAborted;
        var compra = await ObtenerCompraAutorizadaAsync(compraId);
        await ValidarArchivoAsync(archivo);

        var cantidad = await _documentoRepository.CountByCompraIdAsync(compraId);
        if (cantidad >= MaxDocumentosPorCompra)
            throw new BusinessRuleException($"Una compra puede tener como máximo {MaxDocumentosPorCompra} comprobantes adjuntos.");

        var almacenado = await _storage.UploadAsync(tenant, archivo, cancellationToken);
        var documento = new CompraDocumento
        {
            CompraId = compraId,
            NombreOriginal = Path.GetFileName(archivo.FileName),
            ContentType = almacenado.ContentType,
            SizeBytes = almacenado.SizeBytes,
            Url = almacenado.Url,
            PublicId = almacenado.PublicId,
            ResourceType = almacenado.ResourceType,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        try
        {
            await _documentoRepository.AddAsync(documento);
            await _documentoRepository.SaveChangesAsync();
        }
        catch
        {
            await _storage.DeleteAsync(
                tenant,
                almacenado.PublicId,
                almacenado.ResourceType,
                cancellationToken);
            throw;
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Editar,
            $"Comprobante adjuntado a la compra {compra.NumeroCompra}: {documento.NombreOriginal}.",
            documento.Id,
            entidad: "CompraDocumento",
            valoresNuevos: new
            {
                compraId,
                documento.NombreOriginal,
                documento.ContentType,
                documento.SizeBytes
            });

        return ToDto(documento);
    }

    public async Task<(Stream Contenido, string ContentType, string NombreArchivo)?> DownloadAsync(
        int compraId,
        int documentoId)
    {
        var tenant = await ResolverStorageTenantAsync();
        var compra = await ObtenerCompraAutorizadaAsync(compraId);
        var documento = await _documentoRepository.GetByIdAsync(compraId, documentoId);
        if (documento is null) return null;

        var descarga = await _storage.DownloadAsync(tenant, documento.Url, RequestAborted);
        if (descarga is null) return null;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Exportar,
            $"Comprobante descargado de la compra {compra.NumeroCompra}: {documento.NombreOriginal}.",
            documento.Id,
            entidad: "CompraDocumento",
            valoresNuevos: new { compraId, documentoId, documento.NombreOriginal });

        return (descarga.Value.Contenido, descarga.Value.ContentType, documento.NombreOriginal);
    }

    public async Task<bool> DeleteAsync(int compraId, int documentoId)
    {
        var tenant = await ResolverStorageTenantAsync();
        var compra = await ObtenerCompraAutorizadaAsync(compraId);
        var documento = await _documentoRepository.GetByIdAsync(compraId, documentoId);
        if (documento is null) return false;

        await _storage.DeleteAsync(
            tenant,
            documento.PublicId,
            documento.ResourceType,
            RequestAborted);

        documento.Eliminado = true;
        documento.FechaEliminacion = DateTime.UtcNow;
        documento.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        _documentoRepository.Update(documento);
        await _documentoRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Compras,
            AccionPermiso.Editar,
            $"Comprobante retirado de la compra {compra.NumeroCompra}: {documento.NombreOriginal}.",
            documento.Id,
            entidad: "CompraDocumento",
            valoresAnteriores: new
            {
                compraId,
                documento.NombreOriginal,
                documento.ContentType,
                documento.SizeBytes
            },
            valoresNuevos: new { documento.Eliminado, documento.FechaEliminacion });

        return true;
    }

    private CancellationToken RequestAborted =>
        _httpContextAccessor?.HttpContext?.RequestAborted ?? default;

    private Task<StorageTenantContext> ResolverStorageTenantAsync() =>
        StorageTenantContextResolver.ResolverRequeridoAsync(
            _httpContextAccessor,
            _usuarioScopeService,
            RequestAborted);

    private async Task<Compra> ObtenerCompraAutorizadaAsync(int compraId)
    {
        return await _compraRepository.GetByIdAsync(compraId)
            ?? throw new BusinessRuleException("La compra no existe o no pertenece al usuario autenticado.");
    }

    private static async Task ValidarArchivoAsync(IFormFile archivo)
    {
        if (archivo is null || archivo.Length <= 0)
            throw new BusinessRuleException("Selecciona un archivo válido.");
        if (archivo.Length > MaxBytes)
            throw new BusinessRuleException("El comprobante no puede superar 10 MB.");

        var extension = Path.GetExtension(archivo.FileName);
        if (!ExtensionesPermitidas.Contains(extension) || !ContentTypesPermitidos.Contains(archivo.ContentType))
            throw new BusinessRuleException("Solo se permiten archivos JPG, PNG, WebP o PDF.");

        var tipoCoincide = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => string.Equals(archivo.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase),
            ".png" => string.Equals(archivo.ContentType, "image/png", StringComparison.OrdinalIgnoreCase),
            ".webp" => string.Equals(archivo.ContentType, "image/webp", StringComparison.OrdinalIgnoreCase),
            ".pdf" => string.Equals(archivo.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
        if (!tipoCoincide)
            throw new BusinessRuleException("La extensión del archivo no coincide con su tipo de contenido declarado.");

        await using var stream = archivo.OpenReadStream();
        var header = new byte[12];
        var total = 0;
        while (total < header.Length)
        {
            var leidos = await stream.ReadAsync(header.AsMemory(total, header.Length - total));
            if (leidos == 0) break;
            total += leidos;
        }

        var firmaValida = extension.ToLowerInvariant() switch
        {
            ".pdf" => total >= 5 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D,
            ".jpg" or ".jpeg" => total >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => total >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            ".webp" => total >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };
        if (!firmaValida)
            throw new BusinessRuleException("El contenido real del archivo no corresponde a un JPG, PNG, WebP o PDF válido.");
    }

    private static CompraDocumentoDto ToDto(CompraDocumento documento) => new()
    {
        Id = documento.Id,
        CompraId = documento.CompraId,
        NombreOriginal = documento.NombreOriginal,
        ContentType = documento.ContentType,
        SizeBytes = documento.SizeBytes,
        EsImagen = documento.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
        FechaCreacion = documento.FechaCreacion,
        CreadoPorNombreUsuario = documento.CreadoPorNombreUsuario
    };
}
