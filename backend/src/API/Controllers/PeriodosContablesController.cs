using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Route("api/periodos-contables")]
[Authorize]
public sealed class PeriodosContablesController : ControllerBase
{
    private readonly IPeriodoContableService _service;

    public PeriodosContablesController(IPeriodoContableService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll([FromQuery] PeriodoContableQueryDto filter) =>
        Ok(ApiResponse<PagedResult<PeriodoContableDto>>.Ok(await _service.GetPagedAsync(filter)));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound(ApiResponse<object>.Fail("Período contable no encontrado.")) : Ok(ApiResponse<PeriodoContableDto>.Ok(result));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CrearPeriodoContableDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<PeriodoContableDto>.Ok(result));
    }

    [HttpPost("{id:int}/cerrar")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Cerrar)]
    public async Task<IActionResult> Cerrar(int id)
    {
        await _service.CerrarAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "Período contable cerrado correctamente."));
    }
}
