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
[Route("inventario/reportes/kardex")]
public sealed class ReportesInventarioKardexController : ControllerBase
{
    private readonly IMovimientoInventarioService _movimientos;
    private readonly IAlmacenRepository _almacenes;
    private readonly IUsuarioScopeService _usuarioScope;
    private readonly IAuditoriaService _auditoria;

    public ReportesInventarioKardexController(
        IMovimientoInventarioService movimientos,
        IAlmacenRepository almacenes,
        IUsuarioScopeService usuarioScope,
        IAuditoriaService auditoria)
    {
        _movimientos = movimientos;
        _almacenes = almacenes;
        _usuarioScope = usuarioScope;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.ConsultarHistorial)]
    public async Task<IActionResult> Get([FromQuery] ReporteInventarioKardexFiltroDto filtro)
    {
        var error = ReporteInventarioQueryRules.Validate(filtro, "Fecha");
        if (error is not null)
            return BadRequest(ApiResponse<object>.Fail("Consulta de Kardex inválida.", new() { error }));

        if (!await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(filtro, _usuarioScope))
            return Forbid();

        if (filtro.SucursalId.HasValue)
        {
            if (!filtro.AlmacenId.HasValue)
                return BadRequest(ApiResponse<object>.Fail(
                    "Consulta de Kardex inválida.",
                    new() { "Cuando se filtra por SucursalId debe especificarse AlmacenId para preservar un scope físico demostrable." }));

            var almacen = await _almacenes.GetByIdAsync(filtro.AlmacenId.Value);
            if (almacen is null || almacen.SucursalId != filtro.SucursalId.Value)
                return Forbid();
        }

        var query = new MovimientoInventarioQueryDto
        {
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            SortBy = "Fecha",
            SortDirection = filtro.SortDirection,
            ProductoId = filtro.ProductoId,
            ProductoVarianteId = filtro.ProductoVarianteId,
            AlmacenId = filtro.AlmacenId,
            UbicacionAlmacenId = filtro.UbicacionAlmacenId,
            Tipo = filtro.Tipo,
            Causa = filtro.Causa,
            CorrelationId = filtro.CorrelationId,
            OrigenTipo = filtro.OrigenTipo,
            OrigenId = filtro.OrigenId,
            Desde = filtro.Desde,
            Hasta = filtro.Hasta
        };

        var resultado = await _movimientos.GetPagedAsync(query);

        await _auditoria.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.ConsultarHistorial,
            "Consulta de Kardex de inventario.",
            entidad: "KardexInventario",
            valoresNuevos: new
            {
                filtro.ProductoId,
                filtro.ProductoVarianteId,
                filtro.AlmacenId,
                filtro.UbicacionAlmacenId,
                filtro.SucursalId,
                filtro.Tipo,
                filtro.Causa,
                filtro.CorrelationId,
                filtro.OrigenTipo,
                filtro.OrigenId,
                filtro.Desde,
                filtro.Hasta,
                filtro.Page,
                filtro.PageSize
            });

        return Ok(ApiResponse<PagedResult<MovimientoInventarioDto>>.Ok(resultado));
    }
}
