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
[Route("trazabilidad-inventario")]
public sealed class TrazabilidadInventarioController : ControllerBase
{
    private readonly ITrazabilidadInventarioService _service;

    public TrazabilidadInventarioController(ITrazabilidadInventarioService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("variantes/{productoVarianteId:int}/configuracion")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetConfiguracion(int productoVarianteId)
    {
        var configuracion = await _service.GetConfiguracionAsync(productoVarianteId);
        return configuracion is null
            ? NotFound(ApiResponse<object>.Fail("Variante no encontrada."))
            : Ok(ApiResponse<ConfiguracionTrazabilidadVarianteDto>.Ok(configuracion));
    }

    [HttpPut("variantes/{productoVarianteId:int}/configuracion")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> Configurar(
        int productoVarianteId,
        [FromBody] ConfigurarTrazabilidadVarianteRequest request)
    {
        var configuracion = await _service.ConfigurarAsync(productoVarianteId, request);
        return Ok(ApiResponse<ConfiguracionTrazabilidadVarianteDto>.Ok(configuracion, "Configuración de trazabilidad actualizada."));
    }

    [HttpGet("lotes")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetLotes([FromQuery] LoteInventarioQueryDto query)
    {
        var pagina = await _service.GetLotesAsync(query);
        return Ok(ApiResponse<PagedResult<LoteInventarioDto>>.Ok(pagina));
    }

    [HttpGet("lotes/{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetLote(int id)
    {
        var lote = await _service.GetLoteByIdAsync(id);
        return lote is null
            ? NotFound(ApiResponse<object>.Fail("Lote no encontrado."))
            : Ok(ApiResponse<LoteInventarioDto>.Ok(lote));
    }

    [HttpPost("lotes")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Crear)]
    public async Task<IActionResult> CrearLote([FromBody] CrearLoteInventarioRequest request)
    {
        var lote = await _service.CrearLoteAsync(request);
        return CreatedAtAction(nameof(GetLote), new { id = lote.Id },
            ApiResponse<LoteInventarioDto>.Ok(lote, "Lote registrado correctamente."));
    }

    [HttpPut("lotes/{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> ActualizarLote(int id, [FromBody] ActualizarLoteInventarioRequest request)
    {
        var lote = await _service.ActualizarLoteAsync(id, request);
        return Ok(ApiResponse<LoteInventarioDto>.Ok(lote, "Lote actualizado correctamente."));
    }

    [HttpPost("lotes/{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> DesactivarLote(int id)
    {
        var lote = await _service.DesactivarLoteAsync(id);
        return Ok(ApiResponse<LoteInventarioDto>.Ok(lote, "Lote desactivado correctamente."));
    }

    [HttpGet("series")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetSeries([FromQuery] SerieInventarioQueryDto query)
    {
        var pagina = await _service.GetSeriesAsync(query);
        return Ok(ApiResponse<PagedResult<SerieInventarioDto>>.Ok(pagina));
    }

    [HttpGet("series/{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetSerie(int id)
    {
        var serie = await _service.GetSerieByIdAsync(id);
        return serie is null
            ? NotFound(ApiResponse<object>.Fail("Serie no encontrada."))
            : Ok(ApiResponse<SerieInventarioDto>.Ok(serie));
    }

    [HttpPost("series")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Crear)]
    public async Task<IActionResult> CrearSerie([FromBody] CrearSerieInventarioRequest request)
    {
        var serie = await _service.CrearSerieAsync(request);
        return CreatedAtAction(nameof(GetSerie), new { id = serie.Id },
            ApiResponse<SerieInventarioDto>.Ok(serie, "Serie registrada correctamente."));
    }

    [HttpPost("series/{id:int}/baja")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> DarDeBajaSerie(int id)
    {
        var serie = await _service.DarDeBajaSerieAsync(id);
        return Ok(ApiResponse<SerieInventarioDto>.Ok(serie, "Serie dada de baja correctamente."));
    }
}
