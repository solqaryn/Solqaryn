using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("automatizaciones")]
public sealed class AutomatizacionesController : ControllerBase
{
    private readonly IAutomatizacionService _service;

    public AutomatizacionesController(AppDbContext db, ICurrentUserService currentUser)
        => _service = new AutomatizacionService(db, currentUser);

    [HttpGet("configuracion")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetConfiguracion(CancellationToken cancellationToken)
        => Ok(ApiResponse<AutomatizacionConfiguracionDto>.Ok(await _service.GetConfiguracionAsync(cancellationToken)));

    [HttpPut("configuracion")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> UpdateConfiguracion([FromBody] ActualizarAutomatizacionConfiguracionRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<AutomatizacionConfiguracionDto>.Ok(await _service.UpdateConfiguracionAsync(request, cancellationToken), "Preferencias de automatización actualizadas."));

    [HttpGet("sugerencias")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<IActionResult> Sugerencias(CancellationToken cancellationToken)
        => Ok(ApiResponse<AutomatizacionResumenDto>.Ok(await _service.GetSugerenciasAsync(cancellationToken)));

    [HttpGet("autocompletar")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<IActionResult> Autocompletar([FromQuery] string contexto, [FromQuery] string q, CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<AutocompletadoItemDto>>.Ok(await _service.AutocompletarAsync(contexto, q, cancellationToken)));

    [HttpPost("acciones-masivas/previsualizar")]
    [RequierePermiso(ModuloSistema.Dashboard, AccionPermiso.Ver)]
    public async Task<IActionResult> Previsualizar([FromBody] AccionMasivaPreviewRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<AccionMasivaPreviewDto>.Ok(await _service.PrevisualizarAccionMasivaAsync(request, cancellationToken), "Vista previa calculada sin modificar datos."));
}
