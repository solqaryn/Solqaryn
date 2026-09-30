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
[Route("empresas")]
public sealed class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _service;

    public EmpresasController(IEmpresaService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> List([FromQuery] bool? activa, CancellationToken cancellationToken)
    {
        var empresas = await _service.ListAsync(activa, cancellationToken);
        return Ok(ApiResponse<List<EmpresaDto>>.Ok(empresas));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var empresa = await _service.GetByIdAsync(id, cancellationToken);
        if (empresa is null)
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));

        return Ok(ApiResponse<EmpresaDto>.Ok(empresa));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateEmpresaDto dto, CancellationToken cancellationToken)
    {
        var empresa = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = empresa.Id }, ApiResponse<EmpresaDto>.Ok(empresa, "Empresa creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmpresaDto dto, CancellationToken cancellationToken)
    {
        var empresa = await _service.UpdateAsync(id, dto, cancellationToken);
        if (empresa is null)
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));

        return Ok(ApiResponse<EmpresaDto>.Ok(empresa, "Empresa actualizada correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Activar)]
    public Task<IActionResult> Activar(int id, CancellationToken cancellationToken) => CambiarEstado(id, true, cancellationToken);

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Desactivar)]
    public Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken) => CambiarEstado(id, false, cancellationToken);

    private async Task<IActionResult> CambiarEstado(int id, bool activa, CancellationToken cancellationToken)
    {
        var empresa = await _service.CambiarEstadoAsync(id, activa, cancellationToken);
        if (empresa is null)
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));

        return Ok(ApiResponse<EmpresaDto>.Ok(empresa, activa ? "Empresa activada correctamente." : "Empresa desactivada correctamente."));
    }
}
