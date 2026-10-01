using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Mappings;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Services;

public class ProductoService : IProductoService
{
    private const int MaxImagenes = 5;

    private readonly IProductoRepository _repository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IImageStorageService _imageStorage;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly ICatalogoProductoService? _catalogoService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IUsuarioScopeService? _usuarioScopeService;

    public ProductoService(
        IProductoRepository repository,
        ICategoriaRepository categoriaRepository,
        IImageStorageService imageStorage,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        ICatalogoProductoService? catalogoService = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IUsuarioScopeService? usuarioScopeService = null)
    {
        _repository = repository;
        _categoriaRepository = categoriaRepository;
        _imageStorage = imageStorage;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _catalogoService = catalogoService;
        _httpContextAccessor = httpContextAccessor;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<ProductoDto?> GetByIdAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        return producto is null ? null : ProductoMapper.ToDto(producto);
    }

    public async Task<PagedResult<ProductoDto>> GetPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(request);
        return new PagedResult<ProductoDto>
        {
            Items = items.Select(ProductoMapper.ToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductoDto> CreateAsync(CreateProductoDto dto)
    {
        ValidarTipoInventario(dto.TipoInventario);
        var imagenes = dto.Imagenes ?? new List<IFormFile>();
        if (imagenes.Count > MaxImagenes)
            throw new BusinessRuleException($"Un producto puede tener máximo {MaxImagenes} fotos generales.");
        ValidarImagenes(imagenes);

        await ValidarCategoriaAsync(dto.CategoriaId, exigirActiva: true);

        var producto = new Producto
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            TipoInventario = dto.TipoInventario,
            CategoriaId = dto.CategoriaId,
            EsDestacado = dto.EsDestacado,
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        StorageTenantContext? storageTenant = null;
        if (imagenes.Count > 0)
            storageTenant = await ResolverStorageTenantAsync();

        for (var i = 0; i < imagenes.Count; i++)
        {
            var (url, publicId) = await _imageStorage.UploadAsync(
                storageTenant!,
                imagenes[i],
                _httpContextAccessor?.HttpContext?.RequestAborted ?? default);
            producto.Imagenes.Add(new ProductoImagen
            {
                ProductoVarianteId = null,
                Url = url,
                PublicId = publicId,
                Orden = i,
                EsPrincipal = i == 0,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            });
        }

        await _repository.AddAsync(producto);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Crear,
            $"Producto creado: {producto.Nombre}.",
            producto.Id,
            entidad: "Producto",
            valoresNuevos: new
            {
                producto.Nombre,
                producto.TipoInventario,
                producto.CategoriaId,
                producto.EsDestacado,
                ImagenesGenerales = producto.Imagenes.Count(i => i.ProductoVarianteId == null)
            });

        return await GetByIdAsync(producto.Id) ?? ProductoMapper.ToDto(producto);
    }

    public async Task<ProductoDto?> UpdateAsync(int id, UpdateProductoDto dto)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return null;
        var imagenesGenerales = producto.Imagenes.Where(i => i.ProductoVarianteId == null).ToList();
        StorageTenantContext? storageTenant = null;

        var valoresAnteriores = new
        {
            producto.Nombre,
            producto.TipoInventario,
            producto.Descripcion,
            producto.CategoriaId,
            producto.EsDestacado,
            Imagenes = imagenesGenerales.Count,
            ImagenPrincipalId = producto.ImagenPrincipal?.Id
        };

        await ValidarCategoriaAsync(dto.CategoriaId, exigirActiva: false);

        if (dto.TipoInventario.HasValue)
        {
            ValidarTipoInventario(dto.TipoInventario.Value);
            producto.TipoInventario = dto.TipoInventario.Value;
        }
        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        producto.CategoriaId = dto.CategoriaId;
        if (dto.EsDestacado.HasValue)
            producto.EsDestacado = dto.EsDestacado.Value;
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;

        if (dto.ImagenesAEliminarIds is { Count: > 0 })
        {
            var aEliminar = imagenesGenerales
                .Where(i => dto.ImagenesAEliminarIds.Contains(i.Id))
                .ToList();
            if (aEliminar.Count > 0)
                storageTenant = await ResolverStorageTenantAsync();

            foreach (var imagen in aEliminar)
            {
                await _imageStorage.DeleteAsync(
                    storageTenant!,
                    imagen.PublicId,
                    _httpContextAccessor?.HttpContext?.RequestAborted ?? default);
                producto.Imagenes.Remove(imagen);
                imagenesGenerales.Remove(imagen);
            }
        }

        var nuevas = dto.ImagenesNuevas ?? new List<IFormFile>();
        if (imagenesGenerales.Count + nuevas.Count > MaxImagenes)
            throw new BusinessRuleException(
                $"Un producto puede tener máximo {MaxImagenes} fotos generales ({imagenesGenerales.Count} existentes + {nuevas.Count} nuevas excede el límite).");
        ValidarImagenes(nuevas);

        var siguienteOrden = imagenesGenerales.Count == 0
            ? 0
            : imagenesGenerales.Max(i => i.Orden) + 1;
        var yaTienePrincipal = imagenesGenerales.Any(i => i.EsPrincipal);

        if (nuevas.Count > 0 && storageTenant is null)
            storageTenant = await ResolverStorageTenantAsync();

        foreach (var archivo in nuevas)
        {
            var (url, publicId) = await _imageStorage.UploadAsync(
                storageTenant!,
                archivo,
                _httpContextAccessor?.HttpContext?.RequestAborted ?? default);
            var imagen = new ProductoImagen
            {
                ProductoVarianteId = null,
                Url = url,
                PublicId = publicId,
                Orden = siguienteOrden++,
                EsPrincipal = !yaTienePrincipal && imagenesGenerales.Count == 0,
                CreadoPorUsuarioId = _currentUser.UsuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario
            };
            producto.Imagenes.Add(imagen);
            imagenesGenerales.Add(imagen);
            yaTienePrincipal = true;
        }

        if (dto.ImagenPrincipalId.HasValue)
        {
            var nuevaPrincipal = imagenesGenerales
                .FirstOrDefault(i => i.Id == dto.ImagenPrincipalId.Value);
            if (nuevaPrincipal is null)
                throw new BusinessRuleException("La imagen indicada como principal no pertenece a la galería general de este producto.");

            foreach (var imagen in imagenesGenerales)
                imagen.EsPrincipal = false;
            nuevaPrincipal.EsPrincipal = true;
        }
        else if (imagenesGenerales.Count > 0 && !imagenesGenerales.Any(i => i.EsPrincipal))
        {
            imagenesGenerales.OrderBy(i => i.Orden).First().EsPrincipal = true;
        }

        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Editar,
            $"Producto actualizado: {producto.Nombre}.",
            producto.Id,
            entidad: "Producto",
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: new
            {
                producto.Nombre,
                producto.TipoInventario,
                producto.Descripcion,
                producto.CategoriaId,
                producto.EsDestacado,
                Imagenes = imagenesGenerales.Count,
                ImagenPrincipalId = producto.ImagenPrincipal?.Id
            });

        return await GetByIdAsync(producto.Id) ?? ProductoMapper.ToDto(producto);
    }

    public async Task<ProductoDto?> CambiarEstadoAsync(int id, bool activo)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return null;
        if (producto.Activo == activo) return ProductoMapper.ToDto(producto);

        var estadoAnterior = producto.Activo;
        producto.Activo = activo;
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;

        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Producto {(activo ? "activado" : "desactivado")}: {producto.Nombre}.",
            producto.Id,
            entidad: "Producto",
            valoresAnteriores: new { Activo = estadoAnterior },
            valoresNuevos: new { producto.Activo });

        return ProductoMapper.ToDto(producto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var producto = await _repository.GetByIdAsync(id);
        if (producto is null) return false;

        var valoresAnteriores = new
        {
            producto.Nombre,
            producto.Marca,
            producto.Modelo,
            producto.Activo,
            producto.Eliminado,
            ImagenesGenerales = producto.Imagenes.Count(i => i.ProductoVarianteId == null),
            ImagenesVariantes = producto.Imagenes.Count(i => i.ProductoVarianteId != null)
        };

        producto.Activo = false;
        producto.Eliminado = true;
        producto.FechaEliminacion = DateTime.UtcNow;
        producto.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        producto.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        producto.FechaActualizacion = DateTime.UtcNow;

        var guardado = await _repository.SaveChangesAsync();
        if (guardado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Productos,
                AccionPermiso.EliminarLogico,
                $"Producto eliminado lógicamente: {producto.Nombre}.",
                id,
                entidad: "Producto",
                valoresAnteriores: valoresAnteriores,
                valoresNuevos: new
                {
                    producto.Activo,
                    producto.Eliminado,
                    producto.FechaEliminacion
                });
        }

        return guardado;
    }

    private Task<StorageTenantContext> ResolverStorageTenantAsync() =>
        StorageTenantContextResolver.ResolverRequeridoAsync(
            _httpContextAccessor,
            _usuarioScopeService,
            _httpContextAccessor?.HttpContext?.RequestAborted ?? default);

    private async Task ValidarCategoriaAsync(int? categoriaId, bool exigirActiva)
    {
        if (!categoriaId.HasValue) return;
        var categoria = await _categoriaRepository.GetByIdAsync(categoriaId.Value);
        if (categoria is null)
            throw new BusinessRuleException("La categoría seleccionada no existe.");
        if (exigirActiva && !categoria.Activa)
            throw new BusinessRuleException("La categoría seleccionada está inactiva.");
    }

    private async Task ValidarCatalogosAsync(int? colorId, int? tallaId, int? marcaId, int? modeloId)
    {
        if (_catalogoService is not null)
            await _catalogoService.ValidarSeleccionProductoAsync(colorId, tallaId, marcaId, modeloId);
    }

    private async Task<(string Marca, string Modelo)> ResolverMarcaModeloAsync(
        int? marcaId,
        int? modeloId,
        string? marcaLegada,
        string? modeloLegado)
    {
        var marca = marcaLegada?.Trim() ?? string.Empty;
        var modelo = modeloLegado?.Trim() ?? string.Empty;

        if (_catalogoService is not null && marcaId.HasValue)
        {
            var catalogoMarca = await _catalogoService.GetByIdAsync(TipoCatalogoProducto.Marca, marcaId.Value);
            if (catalogoMarca is not null) marca = catalogoMarca.Nombre;
        }

        if (_catalogoService is not null && modeloId.HasValue)
        {
            var catalogoModelo = await _catalogoService.GetByIdAsync(TipoCatalogoProducto.Modelo, modeloId.Value);
            if (catalogoModelo is not null) modelo = catalogoModelo.Nombre;
        }

        return (marca, modelo);
    }

    private static void ValidarTipoInventario(TipoInventario tipoInventario)
    {
        if (!Enum.IsDefined(tipoInventario))
            throw new BusinessRuleException("El tipo de inventario indicado no es válido.");
    }

    private static void ValidarImagenes(IEnumerable<IFormFile> imagenes)
    {
        if (imagenes.Any(imagen => !ImagenValidationHelper.EsImagenValida(imagen)))
            throw new BusinessRuleException("Solo se permiten imágenes JPG, PNG o WebP de hasta 5 MB.");
    }
}
