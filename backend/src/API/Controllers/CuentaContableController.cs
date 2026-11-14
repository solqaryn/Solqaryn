using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces.Services;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("cuentas-contables")]
public sealed class CuentaContableController : ControllerBase
{
    private readonly ICuentaContableService _service;

    public CuentaContableController(ICuentaContableService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll() =>
        Ok(ApiResponse<IReadOnlyList<CuentaContableDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("raices")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetRaices() =>
        Ok(ApiResponse<IReadOnlyList<CuentaContableDto>>.Ok(await _service.GetRaicesAsync()));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Cuenta contable no encontrada",
                detail: $"No existe una cuenta contable con Id {id}.")
            : Ok(ApiResponse<CuentaContableDto>.Ok(result));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateCuentaContableDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CuentaContableDto>.Ok(result));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCuentaContableDto dto) =>
        Ok(ApiResponse<CuentaContableDto>.Ok(await _service.UpdateAsync(id, dto)));
}
