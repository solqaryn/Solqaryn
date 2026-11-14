using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("productos/{productoId:int}/variantes")]
public class ProductoVariantesController : ControllerBase
{
    private readonly IProductoVarianteService _service;
    private readonly IProductoVarianteImagenService _imagenService;

    public ProductoVariantesController(
        IProductoVarianteService service,
        IProductoVarianteImagenService imagenService)
    {
        _service = service;
        _imagenService = imagenService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll(int productoId, [FromQuery] bool incluirInactivas = true) =>
        Ok(ApiResponse<List<ProductoVarianteDto>>.Ok(await _service.GetByProductoIdAsync(productoId, incluirInactivas)));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int productoId, int id)
    {
        var variante = await _service.GetByIdAsync(productoId, id);
        return variante is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<ProductoVarianteDto>.Ok(variante));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Crear)]
    public async Task<IActionResult> Create(int productoId, CreateProductoVarianteDto dto)
    {
        var creada = await _service.CreateAsync(productoId, dto);
        return CreatedAtAction(nameof(GetById), new { productoId, id = creada.Id },
            ApiResponse<ProductoVarianteDto>.Ok(creada, "Variante creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int productoId, int id, UpdateProductoVarianteDto dto)
    {
        var actualizada = await _service.UpdateAsync(productoId, id, dto);
        return actualizada is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<ProductoVarianteDto>.Ok(actualizada, "Variante actualizada correctamente."));
    }

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> CambiarEstado(int productoId, int id, CambiarEstadoProductoVarianteDto dto)
    {
        var actualizada = await _service.CambiarEstadoAsync(productoId, id, dto.Activo);
        return actualizada is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<ProductoVarianteDto>.Ok(actualizada, dto.Activo ? "Variante activada." : "Variante desactivada."));
    }

    [HttpGet("{id:int}/imagenes")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Ver)]
    public async Task<IActionResult> GetImagenes(int productoId, int id)
    {
        var imagenes = await _imagenService.GetAsync(productoId, id);
        return imagenes is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<IReadOnlyList<ProductoImagenDto>>.Ok(imagenes));
    }

    [HttpPost("{id:int}/imagenes")]
    [Consumes("multipart/form-data")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> AgregarImagenes(int productoId, int id, List<IFormFile> archivos)
    {
        var imagenes = await _imagenService.AddAsync(productoId, id, archivos);
        return Ok(ApiResponse<IReadOnlyList<ProductoImagenDto>>.Ok(imagenes, "Imágenes de la variante actualizadas correctamente."));
    }

    [HttpPatch("{id:int}/imagenes/{imagenId:int}/principal")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> EstablecerImagenPrincipal(int productoId, int id, int imagenId)
    {
        var actualizado = await _imagenService.SetPrincipalAsync(productoId, id, imagenId);
        return actualizado
            ? Ok(ApiResponse<object>.Ok(new { }, "Imagen principal de la variante actualizada."))
            : NotFound(ApiResponse<object>.Fail("La imagen o la variante no existe en el ámbito indicado."));
    }

    [HttpDelete("{id:int}/imagenes/{imagenId:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task<IActionResult> EliminarImagen(int productoId, int id, int imagenId)
    {
        var eliminada = await _imagenService.DeleteAsync(productoId, id, imagenId);
        return eliminada
            ? Ok(ApiResponse<object>.Ok(new { }, "Imagen de la variante eliminada."))
            : NotFound(ApiResponse<object>.Fail("La imagen o la variante no existe en el ámbito indicado."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Productos, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int productoId, int id)
    {
        var eliminada = await _service.DeleteAsync(productoId, id);
        return eliminada
            ? Ok(ApiResponse<object>.Ok(new { }, "Variante eliminada lógicamente."))
            : NotFound(ApiResponse<object>.Fail("Variante no encontrada."));
    }
}
