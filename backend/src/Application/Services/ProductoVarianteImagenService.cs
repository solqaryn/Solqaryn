using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Services;

public sealed class ProductoVarianteImagenService : IProductoVarianteImagenService
{
    private const int MaximoImagenesPorVariante = 5;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _varianteRepository;
    private readonly IImageStorageService _storage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUsuarioScopeService? _usuarioScopeService;

    public ProductoVarianteImagenService(
        IProductoRepository productoRepository,
        IProductoVarianteRepository varianteRepository,
        IImageStorageService storage,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        IHttpContextAccessor? httpContextAccessor = null,
        IUsuarioScopeService? usuarioScopeService = null)
    {
        _productoRepository = productoRepository;
        _varianteRepository = varianteRepository;
        _storage = storage;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _httpContextAccessor = httpContextAccessor;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<IReadOnlyList<ProductoImagenDto>?> GetAsync(int productoId, int varianteId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId);
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (producto is null || variante is null || variante.ProductoId != productoId) return null;

        var especificas = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).ToList();
        if (especificas.Count > 0) return Map(especificas);

        // Las imágenes de catálogo son lectura pública autenticada; la mutación
        // (upload/principal/delete) sí queda ligada al tenant server-side.
        return Map(producto.Imagenes.Where(x => x.ProductoVarianteId == null));
    }

    public async Task<IReadOnlyList<ProductoImagenDto>> AddAsync(
        int productoId,
        int varianteId,
        IReadOnlyCollection<IFormFile> archivos)
    {
        if (archivos.Count == 0)
            throw new BusinessRuleException("Selecciona al menos una imagen para la variante.");

        var producto = await _productoRepository.GetByIdAsync(productoId)
            ?? throw new BusinessRuleException("Producto no encontrado.");
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (variante is null || variante.ProductoId != productoId)
            throw new BusinessRuleException("La variante no existe o no pertenece al producto.");

        var actuales = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).ToList();
        if (actuales.Count + archivos.Count > MaximoImagenesPorVariante)
            throw new BusinessRuleException($"Cada variante puede tener hasta {MaximoImagenesPorVariante} imágenes.");

        var tenant = await ResolverStorageTenantAsync();
        var subidas = new List<ProductoImagen>();
        try
        {
            var orden = actuales.Count == 0 ? 0 : actuales.Max(x => x.Orden) + 1;
            foreach (var archivo in archivos)
            {
                var (url, publicId) = await _storage.UploadAsync(tenant, archivo, RequestAborted);
                var imagen = new ProductoImagen
                {
                    ProductoId = productoId,
                    ProductoVarianteId = varianteId,
                    Url = url,
                    PublicId = publicId,
                    Orden = orden++,
                    EsPrincipal = actuales.Count == 0 && subidas.Count == 0,
                    CreadoPorUsuarioId = _currentUser.UsuarioId,
                    CreadoPorNombreUsuario = _currentUser.NombreUsuario
                };
                producto.Imagenes.Add(imagen);
                subidas.Add(imagen);
            }
            await _productoRepository.SaveChangesAsync();
        }
        catch
        {
            foreach (var imagen in subidas)
            {
                try { await _storage.DeleteAsync(tenant, imagen.PublicId, RequestAborted); } catch { }
            }
            throw;
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            $"Se agregaron {subidas.Count} imagen(es) a una variante exacta.",
            varianteId,
            entidad: "ProductoVarianteImagen",
            valoresNuevos: new { productoId, varianteId, imagenes = subidas.Select(x => x.Id).ToArray(), tenant.EmpresaId });

        return Map(producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId));
    }

    public async Task<bool> SetPrincipalAsync(int productoId, int varianteId, int imagenId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId);
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (producto is null || variante is null || variante.ProductoId != productoId) return false;

        var imagenes = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).ToList();
        var seleccionada = imagenes.FirstOrDefault(x => x.Id == imagenId);
        if (seleccionada is null) return false;

        var tenant = await ResolverStorageTenantAsync();
        ExigirImagenDelTenant(tenant, seleccionada);

        foreach (var imagen in imagenes) imagen.EsPrincipal = imagen.Id == imagenId;
        await _productoRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            "Se cambió la imagen principal de una variante.",
            imagenId,
            entidad: "ProductoVarianteImagen",
            valoresNuevos: new { productoId, varianteId, imagenId, tenant.EmpresaId });
        return true;
    }

    public async Task<bool> DeleteAsync(int productoId, int varianteId, int imagenId)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId);
        var variante = await _varianteRepository.GetByIdAsync(varianteId);
        if (producto is null || variante is null || variante.ProductoId != productoId) return false;

        var imagenes = producto.Imagenes.Where(x => x.ProductoVarianteId == varianteId).OrderBy(x => x.Orden).ToList();
        var imagen = imagenes.FirstOrDefault(x => x.Id == imagenId);
        if (imagen is null) return false;

        // Autorizar el tenant y el locator antes de cualquier mutación de metadata.
        var tenant = await ResolverStorageTenantAsync();
        ExigirImagenDelTenant(tenant, imagen);
        await _storage.DeleteAsync(tenant, imagen.PublicId, RequestAborted);

        var eraPrincipal = imagen.EsPrincipal;
        producto.Imagenes.Remove(imagen);
        if (eraPrincipal)
        {
            var siguiente = imagenes.FirstOrDefault(x => x.Id != imagenId);
            if (siguiente is not null) siguiente.EsPrincipal = true;
        }
        await _productoRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            "Se eliminó una imagen de una variante exacta.",
            imagenId,
            entidad: "ProductoVarianteImagen",
            valoresNuevos: new { productoId, varianteId, imagenId, tenant.EmpresaId });
        return true;
    }

    private CancellationToken RequestAborted =>
        _httpContextAccessor?.HttpContext?.RequestAborted ?? default;

    private Task<StorageTenantContext> ResolverStorageTenantAsync() =>
        StorageTenantContextResolver.ResolverRequeridoAsync(
            _httpContextAccessor,
            _usuarioScopeService,
            RequestAborted);

    private static void ExigirImagenDelTenant(StorageTenantContext tenant, ProductoImagen imagen)
    {
        var marker = $"/empresas/{tenant.EmpresaId}/";
        var publicId = $"/{imagen.PublicId.Trim().Trim('/')}";
        if (!publicId.Contains(marker, StringComparison.Ordinal))
        {
            throw new ForbiddenAccessException(
                "La imagen seleccionada no pertenece al contexto tenant verificado.");
        }
    }

    private static IReadOnlyList<ProductoImagenDto> Map(IEnumerable<ProductoImagen> imagenes) =>
        imagenes.OrderBy(x => x.Orden).Select(x => new ProductoImagenDto
        {
            Id = x.Id,
            Url = x.Url,
            Orden = x.Orden,
            EsPrincipal = x.EsPrincipal,
            ProductoVarianteId = x.ProductoVarianteId
        }).ToList();
}
