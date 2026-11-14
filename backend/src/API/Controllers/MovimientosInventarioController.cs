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
[Route("inventario/movimientos")]
public class MovimientosInventarioController : ControllerBase
{
    private readonly IMovimientoInventarioService _service;

    public MovimientosInventarioController(IMovimientoInventarioService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] int? productoId, [FromQuery] string? tipo,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var movimientos = await _service.GetFilteredAsync(productoId, tipo, desde, hasta);
        return Ok(ApiResponse<List<MovimientoInventarioDto>>.Ok(movimientos));
    }

    [HttpGet("paged")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] MovimientoInventarioQueryDto query)
    {
        var resultado = await _service.GetPagedAsync(query);
        return Ok(ApiResponse<PagedResult<MovimientoInventarioDto>>.Ok(resultado));
    }
}
