using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("contabilizacion")]
public sealed class ContabilizacionController : ControllerBase
{
    private readonly IContabilizacionService _service;

    public ContabilizacionController(AppDbContext db, IAuditoriaService auditoria)
    {
        var writer = new AsientoContableWriter(db, auditoria);
        _service = new ContabilizacionService(db, writer);
    }

    [HttpPost("eventos")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Contabilizar(
        [FromBody] EventoContableDto evento,
        CancellationToken cancellationToken)
    {
        var result = await _service.ContabilizarAsync(evento, cancellationToken);

        return result.Created
            ? StatusCode(StatusCodes.Status201Created, ApiResponse<AsientoContableDto>.Ok(result.Asiento))
            : Ok(ApiResponse<AsientoContableDto>.Ok(result.Asiento));
    }
}
