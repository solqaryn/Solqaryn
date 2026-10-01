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
[Route("secuencias-documento")]
public sealed class SecuenciasDocumentoController : ControllerBase
{
    private readonly ISecuenciaDocumentoService _service;

    public SecuenciasDocumentoController(AppDbContext db, IUsuarioScopeService usuarioScope)
    {
        _service = new SecuenciaDocumentoService(db, usuarioScope);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> Get(
        [FromQuery] int empresaId,
        [FromQuery] int? sucursalId,
        [FromQuery] string tipoDocumento,
        CancellationToken cancellationToken)
    {
        var secuencia = await _service.ObtenerAsync(
            empresaId,
            sucursalId,
            tipoDocumento,
            cancellationToken);

        return Ok(ApiResponse<SecuenciaDocumentoConsultaDto>.Ok(secuencia));
    }

    [HttpPost("siguiente")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> ReservarSiguiente(
        [FromBody] ReservarSecuenciaDocumentoRequest request,
        CancellationToken cancellationToken)
    {
        var reserva = await _service.ReservarSiguienteAsync(request, cancellationToken);
        return Ok(ApiResponse<SecuenciaDocumentoSiguienteDto>.Ok(
            reserva,
            "Número reservado correctamente."));
    }
}
