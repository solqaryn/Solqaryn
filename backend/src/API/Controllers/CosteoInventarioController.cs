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
[Route("costeo-inventario")]
public sealed class CosteoInventarioController : ControllerBase
{
    private readonly IPoliticaCosteoInventarioService _service;

    public CosteoInventarioController(IPoliticaCosteoInventarioService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("politica-vigente")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetVigente()
    {
        var politica = await _service.GetVigenteAsync();
        return Ok(ApiResponse<PoliticaCosteoInventarioDto>.Ok(politica));
    }

    [HttpGet("politicas")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetHistorial([FromQuery] PoliticaCosteoInventarioQueryDto query)
    {
        var pagina = await _service.GetHistorialAsync(query);
        return Ok(ApiResponse<PagedResult<PoliticaCosteoInventarioDto>>.Ok(pagina));
    }

    [HttpGet("metodos")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetMetodos()
    {
        var metodos = await _service.GetMetodosAsync();
        return Ok(ApiResponse<IReadOnlyList<MetodoCosteoInventarioDto>>.Ok(metodos));
    }

    [HttpPut("politica-vigente")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> Cambiar([FromBody] CambiarPoliticaCosteoInventarioDto dto)
    {
        var politica = await _service.CambiarAsync(dto);
        return Ok(ApiResponse<PoliticaCosteoInventarioDto>.Ok(
            politica,
            "Política de costeo actualizada correctamente."));
    }
}
