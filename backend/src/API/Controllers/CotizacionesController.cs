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
[Route("cotizaciones")]
public sealed class CotizacionesController : ControllerBase
{
    private readonly ICotizacionService _service;

    public CotizacionesController(ICotizacionService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] CotizacionFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<CotizacionDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var cotizacion = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<CotizacionDto>.Ok(cotizacion));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Crear([FromBody] CreateCotizacionDto dto)
    {
        var creada = await _service.CrearAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<CotizacionDto>.Ok(creada, "Cotización creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Editar)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] UpdateCotizacionDto dto)
    {
        dto.Id = id;
        var actualizada = await _service.ActualizarAsync(dto);
        return Ok(ApiResponse<CotizacionDto>.Ok(actualizada, "Cotización actualizada correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.EliminarPermanente)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _service.EliminarAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Cotización eliminada correctamente."));
    }

    [HttpPost("{id:int}/enviar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Enviar(int id)
    {
        var cotizacion = await _service.EnviarAsync(id);
        return Ok(ApiResponse<CotizacionDto>.Ok(cotizacion, "Cotización enviada correctamente."));
    }

    [HttpPost("{id:int}/aceptar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Aprobar)]
    public async Task<IActionResult> Aceptar(int id)
    {
        var cotizacion = await _service.AceptarAsync(id);
        return Ok(ApiResponse<CotizacionDto>.Ok(cotizacion, "Cotización aceptada correctamente."));
    }

    [HttpPost("{id:int}/rechazar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Rechazar)]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarCotizacionDto dto)
    {
        var cotizacion = await _service.RechazarAsync(id, dto.Motivo);
        return Ok(ApiResponse<CotizacionDto>.Ok(cotizacion, "Cotización rechazada correctamente."));
    }

    [HttpPost("{id:int}/convertir")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Convertir(int id)
    {
        var cotizacion = await _service.ConvertirAsync(id);
        return Ok(ApiResponse<CotizacionDto>.Ok(cotizacion, "Cotización convertida correctamente."));
    }

    [HttpPost("{id:int}/duplicar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Duplicar)]
    public async Task<IActionResult> Duplicar(int id)
    {
        var duplicada = await _service.DuplicarComoBorradorAsync(id);
        return CreatedAtAction(
            nameof(GetById),
            new { id = duplicada.Id },
            ApiResponse<CotizacionDto>.Ok(duplicada, "Cotización duplicada como borrador."));
    }
}