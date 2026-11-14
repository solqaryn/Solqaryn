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
[Route("existencias-variante")]
public sealed class ExistenciasVarianteController : ControllerBase
{
    private readonly IExistenciaVarianteService _service;

    public ExistenciasVarianteController(IExistenciaVarianteService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] ExistenciaVarianteFiltroDto filtro)
    {
        var pagina = await _service.BuscarAsync(filtro);
        return Ok(ApiResponse<PagedResult<ExistenciaVarianteDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var existencia = await _service.GetByIdAsync(id);
        if (existencia is null)
            return NotFound(ApiResponse<object>.Fail("Existencia no encontrada."));

        return Ok(ApiResponse<ExistenciaVarianteDto>.Ok(existencia));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateExistenciaVarianteDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<ExistenciaVarianteDto>.Ok(creada, "Existencia creada correctamente."));
    }

    [HttpPut("{id:int}/configuracion")]
    [RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Editar)]
    public async Task<IActionResult> UpdateConfiguracion(
        int id,
        [FromBody] UpdateExistenciaVarianteConfiguracionDto dto)
    {
        var actualizada = await _service.UpdateConfiguracionAsync(id, dto);
        if (actualizada is null)
            return NotFound(ApiResponse<object>.Fail("Existencia no encontrada."));

        return Ok(ApiResponse<ExistenciaVarianteDto>.Ok(actualizada, "Configuración de existencia actualizada correctamente."));
    }
}
