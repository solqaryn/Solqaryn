using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Superficie API mínima N6.9.D. empresaId expresa selección de tenant, no
/// autoridad; ISuscripcionesSaaSService verifica membresía server-side antes de
/// consultar o mutar persistencia.
/// </summary>
[ApiController]
[Authorize]
[Route("saas/tenants/{empresaId:int}")]
public sealed class SuscripcionesSaaSController : ControllerBase
{
    private readonly ISuscripcionesSaaSService _service;

    /// <summary>
    /// Composition root explícito N6.9.D usando dependencias ya registradas en el
    /// host. Se marca para que ActivatorUtilities no elija el constructor de tests.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public SuscripcionesSaaSController(
        AppDbContext db,
        IUsuarioScopeService usuarioScopeService,
        IAuditoriaService auditoriaService)
        : this(new SuscripcionesSaaSService(
            new SuscripcionesSaaSRepository(db),
            usuarioScopeService,
            auditoriaService))
    {
    }

    /// <summary>
    /// Constructor inyectable para pruebas dirigidas del contrato HTTP.
    /// </summary>
    public SuscripcionesSaaSController(ISuscripcionesSaaSService service)
    {
        _service = service;
    }

    [HttpPost("onboarding")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Crear)]
    public async Task<IActionResult> Onboarding(
        int empresaId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] OnboardingSuscripcionSaaSRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Idempotency-Key requerida",
                Detail = "Idempotency-Key es obligatorio para onboarding.",
                Type = "https://httpstatuses.com/400"
            };
            problem.Extensions["code"] = SuscripcionSaaSErrorCodes.IdempotencyKeyRequerida;

            return BadRequest(problem);
        }

        var suscripcion = await _service.OnboardingAsync(
            empresaId,
            request,
            idempotencyKey.Trim(),
            cancellationToken);

        return Ok(ApiResponse<SuscripcionSaaSDto>.Ok(suscripcion));
    }

    [HttpGet("suscripcion")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> ObtenerActual(
        int empresaId,
        [FromQuery] DateTime? instanteUtc,
        CancellationToken cancellationToken)
    {
        var suscripcion = await _service.ObtenerActualAsync(
            empresaId,
            instanteUtc,
            cancellationToken);

        return Ok(ApiResponse<SuscripcionSaaSDto>.Ok(suscripcion));
    }

    [HttpGet("limites")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> ObtenerLimites(
        int empresaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 50,
        [FromQuery] string? clave = null,
        CancellationToken cancellationToken = default)
    {
        var query = new LimitesSuscripcionSaaSQuery(pagina, tamanoPagina, clave).Normalizada();
        var limites = await _service.ObtenerLimitesAsync(empresaId, query, cancellationToken);
        return Ok(ApiResponse<PaginaSuscripcionSaaSDto<LimiteSuscripcionSaaSDto>>.Ok(limites));
    }

    [HttpGet("modulos/{moduloClave}/entitlement")]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> EvaluarModulo(
        int empresaId,
        string moduloClave,
        [FromQuery] DateTime? instanteUtc,
        CancellationToken cancellationToken)
    {
        var entitlement = await _service.EvaluarModuloAsync(
            empresaId,
            moduloClave,
            instanteUtc,
            cancellationToken);

        return Ok(ApiResponse<EntitlementModuloSaaSDto>.Ok(entitlement));
    }
}
