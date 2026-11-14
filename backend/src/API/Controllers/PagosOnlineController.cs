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

namespace Solqaryn.API.Controllers;

/// <summary>
/// N7.8.D — boundary provider-agnostic para pagos online.
/// La API opera sólo con referencias opacas del proveedor; no recibe ni persiste PAN/CVV.
/// </summary>
[ApiController]
[Authorize]
[Route("pagos-online")]
public sealed class PagosOnlineController : ControllerBase
{
    private readonly IPagoOnlineService _service;
    private readonly IUsuarioScopeService _usuarioScope;

    public PagosOnlineController(
        AppDbContext db,
        IFacturaRepository facturas,
        IEnumerable<IPagoOnlineProvider> providers,
        ICurrentUserService currentUser,
        IUsuarioScopeService usuarioScope)
    {
        // Composición local deliberada: el puerto de proveedor puede permanecer vacío
        // hasta que una integración externa sea configurada; no requiere secretos ni
        // registros DI de proveedores inexistentes para consultar/listar pagos.
        _service = new PagoOnlineService(
            new PagoOnlineRepository(db, facturas),
            providers,
            currentUser);
        _usuarioScope = usuarioScope;
    }

    [HttpPost("iniciar")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Aplicar)]
    public async Task<IActionResult> Iniciar(
        [FromBody] IniciarPagoOnlineDto solicitud,
        CancellationToken cancellationToken)
    {
        if (!await TieneScopeTenantAsync(solicitud.EmpresaId, cancellationToken))
            return Forbid();

        var claveIdempotencia = Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(claveIdempotencia) || claveIdempotencia.Length is < 16 or > 128)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency-Key inválida.",
                detail: "Debe enviar una Idempotency-Key de entre 16 y 128 caracteres.");
        }

        var resultado = await _service.IniciarAsync(solicitud, claveIdempotencia, cancellationToken);
        if (resultado.Reutilizado)
        {
            Response.Headers["Idempotency-Replayed"] = "true";
            return Ok(ApiResponse<InicioPagoOnlineResultadoDto>.Ok(
                resultado,
                "La solicitud ya había sido procesada con la misma clave de idempotencia."));
        }

        return CreatedAtAction(
            nameof(Obtener),
            new { id = resultado.Pago.Id, empresaId = solicitud.EmpresaId },
            ApiResponse<InicioPagoOnlineResultadoDto>.Ok(resultado, "Pago online iniciado."));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> Obtener(
        int id,
        [FromQuery] int empresaId,
        CancellationToken cancellationToken)
    {
        if (!await TieneScopeTenantAsync(empresaId, cancellationToken))
            return Forbid();

        var pago = await _service.ObtenerAsync(empresaId, id, cancellationToken);
        if (pago is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Pago online no encontrado.",
                detail: "No existe un pago online con ese identificador dentro de la empresa indicada.");
        }

        return Ok(ApiResponse<PagoOnlineDto>.Ok(pago));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> Listar(
        [FromQuery] int empresaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 25,
        [FromQuery] EstadoPagoOnline? estado = null,
        [FromQuery] int? facturaId = null,
        [FromQuery] string? proveedor = null,
        CancellationToken cancellationToken = default)
    {
        if (!await TieneScopeTenantAsync(empresaId, cancellationToken))
            return Forbid();

        var resultado = await _service.ListarAsync(
            empresaId,
            pagina,
            tamanoPagina,
            estado,
            facturaId,
            proveedor,
            cancellationToken);

        return Ok(ApiResponse<PaginaPagosOnlineDto>.Ok(resultado));
    }

    private async Task<bool> TieneScopeTenantAsync(int empresaId, CancellationToken cancellationToken)
    {
        if (empresaId <= 0)
            return false;

        return await _usuarioScope.ObtenerActualAsync(empresaId, cancellationToken) is not null;
    }
}
