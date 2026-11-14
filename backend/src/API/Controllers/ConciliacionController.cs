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
[Route("conciliacion")]
public sealed class ConciliacionController : ControllerBase
{
    private readonly IThreeWayMatchService _service;

    public ConciliacionController(IThreeWayMatchService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("ordenes-compra/{ordenCompraId:int}/three-way-match")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> EvaluarThreeWayMatch(
        [FromRoute] int ordenCompraId,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.EvaluarAsync(ordenCompraId, cancellationToken);
        return Ok(ApiResponse<ThreeWayMatchResultDto>.Ok(resultado));
    }
}
