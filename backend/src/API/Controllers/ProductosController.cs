using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("productos")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly IProductoVarianteService _varianteService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _imageStorageService;
    private readonly IAuditoriaService _auditoria;

    public ProductosController(
        IProductoService productoService,
        IProductoVarianteService varianteService,
        IUnitOfWork unitOfWork,
        IImageStorageService imageStorageService,
        IAuditoriaService auditoria)
    {
        _productoService = productoService;
        _varianteService = varianteService;
        _unitOfWork = unitOfWork;
        _imageStorageService = imageStorageService;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] ProductoPagedRequest request)
    {
        var resultado = await _productoService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<ProductoDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoDto>.Ok(producto));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromForm] CreateProductoDto dto)
    {
        ProductoDto? creado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (variantes, forzarTecnica) = ResolverVariantesSolicitud(dto);
            creado = await _productoService.CreateAsync(dto);
            await SincronizarVariantesAsync(creado.Id, variantes, Array.Empty<ProductoVarianteDto>(), forzarTecnica);
        });

        var resultado = await _productoService.GetByIdAsync(creado!.Id) ?? creado;
        return CreatedAtAction(nameof(GetById), new { id = resultado.Id },
            ApiResponse<ProductoDto>.Ok(resultado, "Producto y variantes de existencia creados correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateProductoDto dto)
    {
        ProductoDto? actualizado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (variantes, forzarTecnica) = ResolverVariantesSolicitud(dto);
            var existentes = await _varianteService.GetByProductoIdAsync(id, incluirInactivas: true);
            actualizado = await _productoService.UpdateAsync(id, dto);
            if (actualizado is not null)
            {
                await SincronizarVariantesAsync(id, variantes, existentes, forzarTecnica);
                await _varianteService.SincronizarTecnicaConProductoAsync(id);
            }
        });

        if (actualizado is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        var resultado = await _productoService.GetByIdAsync(id) ?? actualizado;
        return Ok(ApiResponse<ProductoDto>.Ok(resultado, "Producto y variantes de existencia actualizados correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        ProductoDto? producto = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            producto = await _productoService.CambiarEstadoAsync(id, true);
            if (producto is not null)
                await _varianteService.SincronizarTecnicaConProductoAsync(id);
        });
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoDto>.Ok(producto, "Producto activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        ProductoDto? producto = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            producto = await _productoService.CambiarEstadoAsync(id, false);
            if (producto is not null)
                await _varianteService.SincronizarTecnicaConProductoAsync(id);
        });
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<ProductoDto>.Ok(producto, "Producto desactivado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = false;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _varianteService.EliminarTecnicaConProductoAsync(id);
            eliminado = await _productoService.DeleteAsync(id);
        });
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        return Ok(ApiResponse<object>.Ok(new { }, "Producto eliminado lógicamente. Su historial permanece protegido."));
    }

    [HttpGet("{id:int}/imagenes/{imagenId:int}/descargar")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarImagen(int id, int imagenId)
    {
        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        var imagen = producto.Imagenes.FirstOrDefault(i => i.Id == imagenId);
        if (imagen is null)
            return NotFound(ApiResponse<object>.Fail("La imagen no existe o no pertenece a este producto."));

        var descarga = await _imageStorageService.DownloadAsync(imagen.Url);
        if (descarga is null)
            return NotFound(ApiResponse<object>.Fail("El archivo de la imagen ya no está disponible."));

        var (contenido, contentType) = descarga.Value;
        var extension = contentType.Contains("png") ? "png" : contentType.Contains("webp") ? "webp" : "jpg";
        var nombreArchivo = $"{producto.Nombre}-{imagen.Orden + 1}.{extension}".Replace(" ", "_");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Exportar,
            $"Imagen descargada del producto: {producto.Nombre}.",
            imagenId,
            entidad: "ProductoImagen",
            valoresNuevos: new { productoId = id, imagenId, nombreArchivo });
        return File(contenido, contentType, nombreArchivo);
    }

    [HttpGet("{id:int}/imagenes/descargar-todas")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarTodasLasImagenes(int id)
    {
        var producto = await _productoService.GetByIdAsync(id);
        if (producto is null)
            return NotFound(ApiResponse<object>.Fail("Producto no encontrado."));

        if (producto.Imagenes.Count == 0)
            return NotFound(ApiResponse<object>.Fail("Este producto no tiene imágenes."));

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contador = 1;
            foreach (var imagen in producto.Imagenes.OrderBy(i => i.Orden))
            {
                var descarga = await _imageStorageService.DownloadAsync(imagen.Url);
                if (descarga is null) continue;

                var (contenido, contentType) = descarga.Value;
                var extension = contentType.Contains("png") ? "png" : contentType.Contains("webp") ? "webp" : "jpg";
                var entry = archive.CreateEntry($"{contador}.{extension}", CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await contenido.CopyToAsync(entryStream);
                contador++;
            }
        }

        memoryStream.Position = 0;
        var nombreZip = $"{producto.Nombre}-imagenes.zip".Replace(" ", "_");
        await _auditoria.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Exportar,
            $"Galería descargada del producto: {producto.Nombre}.",
            id,
            entidad: "Producto",
            valoresNuevos: new
            {
                productoId = id,
                imagenes = producto.Imagenes.Count,
                nombreArchivo = nombreZip
            });

        return File(memoryStream.ToArray(), "application/zip", nombreZip);
    }

    private async Task SincronizarVariantesAsync(
        int productoId,
        IReadOnlyCollection<ProductoVarianteFormularioDto> solicitadas,
        IReadOnlyCollection<ProductoVarianteDto> existentes,
        bool forzarTecnica = false)
    {
        var tecnica = existentes.SingleOrDefault(v => v.EsTecnica);
        var comercialesExistentes = existentes.Where(v => !v.EsTecnica).ToList();

        if (forzarTecnica || EsSolicitudTecnica(solicitadas))
        {
            if (comercialesExistentes.Any(v => v.Cantidad != 0))
                throw new BusinessRuleException("No puedes convertir el producto en simple mientras alguna variante comercial tenga existencias.");
            foreach (var comercial in comercialesExistentes)
                await _varianteService.DeleteAsync(productoId, comercial.Id);
            await _varianteService.SincronizarTecnicaAsync(productoId, solicitadas.Single());
            return;
        }

        if (solicitadas.Count == 0)
        {
            if (comercialesExistentes.Any(v => v.Cantidad != 0))
                throw new BusinessRuleException("No puedes convertir el producto en simple mientras alguna variante comercial tenga existencias.");
            foreach (var comercial in comercialesExistentes)
                await _varianteService.DeleteAsync(productoId, comercial.Id);
            await _varianteService.AsegurarTecnicaAsync(productoId);
            return;
        }

        if (tecnica is not null)
            await _varianteService.RetirarTecnicaParaConversionAsync(productoId);

        if (solicitadas.Any(v => !TieneDimension(v)))
            throw new BusinessRuleException("Cada variante comercial debe definir al menos Marca, Modelo, Color o Talla.");
        if (solicitadas.Any(v => v.Cantidad < 0 || v.UmbralStockBajo < 0))
            throw new BusinessRuleException("La cantidad y el umbral de cada variante no pueden ser negativos.");
        if (solicitadas.Any(v => v.Costo < 0 || v.Precio <= 0))
            throw new BusinessRuleException("Cada variante debe tener costo no negativo y precio mayor que cero.");

        static string Clave(ProductoVarianteFormularioDto v) =>
            $"{v.MarcaId ?? 0}:{v.ModeloId ?? 0}:{v.ColorId ?? 0}:{v.TallaId ?? 0}";
        if (solicitadas.GroupBy(Clave).Any(g => g.Count() > 1))
            throw new BusinessRuleException("No puedes registrar dos veces la misma combinación de Marca, Modelo, Color y Talla.");

        var skus = solicitadas.Where(v => !string.IsNullOrWhiteSpace(v.Sku)).Select(v => v.Sku!.Trim().ToUpperInvariant()).ToList();
        if (skus.Distinct(StringComparer.OrdinalIgnoreCase).Count() != skus.Count)
            throw new BusinessRuleException("No puedes repetir un SKU dentro del mismo producto.");
        var codigos = solicitadas.Where(v => !string.IsNullOrWhiteSpace(v.CodigoBarras)).Select(v => v.CodigoBarras!.Trim()).ToList();
        if (codigos.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codigos.Count)
            throw new BusinessRuleException("No puedes repetir un código de barras dentro del mismo producto.");

        var idsSolicitados = solicitadas.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();
        foreach (var existente in comercialesExistentes.Where(v => !idsSolicitados.Contains(v.Id)))
        {
            if (existente.Cantidad > 0)
                throw new BusinessRuleException($"No puedes retirar la variante '{existente.Etiqueta}' porque todavía tiene {existente.Cantidad} unidades.");
            await _varianteService.DeleteAsync(productoId, existente.Id);
        }

        foreach (var solicitada in solicitadas)
        {
            var existente = solicitada.Id.HasValue ? comercialesExistentes.FirstOrDefault(v => v.Id == solicitada.Id.Value) : null;
            if (solicitada.Id.HasValue && existente is null)
                throw new BusinessRuleException("Una de las variantes indicadas no pertenece al producto.");

            if (existente is not null && solicitada.Cantidad != existente.Cantidad)
                throw new BusinessRuleException($"El stock de '{existente.Etiqueta}' debe cambiarse mediante Ajustar inventario para conservar trazabilidad.");

            if (existente is null)
            {
                var creada = await _varianteService.CreateAsync(productoId, new CreateProductoVarianteDto
                {
                    MarcaId = solicitada.MarcaId,
                    ModeloId = solicitada.ModeloId,
                    ColorId = solicitada.ColorId,
                    TallaId = solicitada.TallaId,
                    Sku = solicitada.Sku,
                    CodigoBarras = solicitada.CodigoBarras,
                    Cantidad = solicitada.Cantidad,
                    UmbralStockBajo = solicitada.UmbralStockBajo,
                    Costo = solicitada.Costo,
                    Precio = solicitada.Precio
                });
                if (!solicitada.Activo)
                    await _varianteService.CambiarEstadoAsync(productoId, creada.Id, false);
            }
            else
            {
                var actualizada = await _varianteService.UpdateAsync(productoId, existente.Id, new UpdateProductoVarianteDto
                {
                    MarcaId = solicitada.MarcaId,
                    ModeloId = solicitada.ModeloId,
                    ColorId = solicitada.ColorId,
                    TallaId = solicitada.TallaId,
                    Sku = solicitada.Sku,
                    CodigoBarras = solicitada.CodigoBarras,
                    Cantidad = existente.Cantidad,
                    UmbralStockBajo = solicitada.UmbralStockBajo,
                    Costo = solicitada.Costo,
                    Precio = solicitada.Precio
                }) ?? throw new BusinessRuleException("No se pudo actualizar una de las variantes del producto.");
                if (actualizada.Activo != solicitada.Activo)
                    await _varianteService.CambiarEstadoAsync(productoId, existente.Id, solicitada.Activo);
            }
        }
    }

    private static (IReadOnlyCollection<ProductoVarianteFormularioDto> Variantes, bool ForzarTecnica)
        ResolverVariantesSolicitud(CreateProductoDto dto)
    {
        if (dto.Variantes.Count > 0) return (dto.Variantes, false);
        return (new[] { DesdeCompatibilidad(dto.MarcaId, dto.ModeloId, dto.ColorId, dto.TallaId, dto.Cantidad, dto.UmbralStockBajo, dto.Costo, dto.Precio) }, true);
    }

    private static (IReadOnlyCollection<ProductoVarianteFormularioDto> Variantes, bool ForzarTecnica)
        ResolverVariantesSolicitud(UpdateProductoDto dto)
    {
        if (dto.Variantes.Count > 0) return (dto.Variantes, false);
        return (new[] { DesdeCompatibilidad(dto.MarcaId, dto.ModeloId, dto.ColorId, dto.TallaId, dto.Cantidad, dto.UmbralStockBajo, dto.Costo, dto.Precio) }, true);
    }

    private static ProductoVarianteFormularioDto DesdeCompatibilidad(
        int? marcaId, int? modeloId, int? colorId, int? tallaId,
        int cantidad, int umbral, decimal costo, decimal precio) => new()
    {
        MarcaId = marcaId,
        ModeloId = modeloId,
        ColorId = colorId,
        TallaId = tallaId,
        Cantidad = cantidad,
        UmbralStockBajo = umbral,
        Costo = costo,
        Precio = precio,
        Activo = true
    };

    private static bool EsSolicitudTecnica(IReadOnlyCollection<ProductoVarianteFormularioDto> variantes)
    {
        if (variantes.Count != 1) return false;
        var variante = variantes.Single();
        return !TieneDimension(variante)
            && string.IsNullOrWhiteSpace(variante.Sku)
            && string.IsNullOrWhiteSpace(variante.CodigoBarras);
    }

    private static bool TieneDimension(ProductoVarianteFormularioDto variante) =>
        variante.MarcaId.HasValue || variante.ModeloId.HasValue || variante.ColorId.HasValue || variante.TallaId.HasValue;
}
