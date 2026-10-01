using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("estados-financieros")]
public sealed class EstadosFinancierosController : ControllerBase
{
    private readonly IEstadoFinancieroService _service;

    public EstadosFinancierosController(IEstadoFinancieroService service)
    {
        _service = service;
    }

    [HttpGet("{tipo:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> Generar(
        int tipo,
        [FromQuery] EstadoFinancieroFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(TipoEstadoFinanciero), tipo))
            return BadRequest(ApiResponse<object>.Fail("Tipo de estado financiero no válido."));

        var resultado = await _service.GenerarAsync((TipoEstadoFinanciero)tipo, filtro, cancellationToken);
        return Ok(ApiResponse<EstadoFinancieroDto>.Ok(resultado));
    }
}
