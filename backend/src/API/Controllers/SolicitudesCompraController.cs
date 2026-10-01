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
[Route("solicitudes-compra")]
public sealed class SolicitudesCompraController : ControllerBase
{
    private readonly ISolicitudCompraService _service;

    public SolicitudesCompraController(ISolicitudCompraService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] SolicitudCompraFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<SolicitudCompraDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var solicitud = await _service.GetByIdAsync(id);
        return solicitud is null
            ? NotFound(ApiResponse<object>.Fail("Solicitud de compra no encontrada."))
            : Ok(ApiResponse<SolicitudCompraDto>.Ok(solicitud));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateSolicitudCompraDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<SolicitudCompraDto>.Ok(creada, "Solicitud de compra creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSolicitudCompraDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<SolicitudCompraDto>.Ok(actualizada, "Solicitud de compra actualizada correctamente."));
    }

    [HttpPost("{id:int}/enviar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Enviar(int id)
    {
        var solicitud = await _service.EnviarAsync(id);
        return Ok(ApiResponse<SolicitudCompraDto>.Ok(solicitud, "Solicitud enviada a aprobación correctamente."));
    }

    [HttpPost("{id:int}/aprobar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Aprobar)]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _service.AprobarAsync(id);
        return Ok(ApiResponse<SolicitudCompraDto>.Ok(solicitud, "Solicitud de compra aprobada correctamente."));
    }

    [HttpPost("{id:int}/rechazar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Rechazar)]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarSolicitudCompraDto dto)
    {
        var solicitud = await _service.RechazarAsync(id, dto);
        return Ok(ApiResponse<SolicitudCompraDto>.Ok(solicitud, "Solicitud de compra rechazada correctamente."));
    }
}
