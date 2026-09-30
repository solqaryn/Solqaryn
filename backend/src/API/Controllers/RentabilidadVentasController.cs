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
[Route("ventas/rentabilidad")]
public sealed class RentabilidadVentasController : ControllerBase
{
    private readonly IRentabilidadVentasService _service;
    private readonly IAuditoriaService? _auditoria;

    public RentabilidadVentasController(
        IRentabilidadVentasService service,
        IAuditoriaService? auditoria = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _auditoria = auditoria;
    }

    [HttpGet("vendedores")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetVendedores([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Vendedor, cancellationToken);

    [HttpGet("clientes")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetClientes([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Cliente, cancellationToken);

    [HttpGet("productos")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetProductos([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Producto, cancellationToken);

    [HttpGet("categorias")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public Task<IActionResult> GetCategorias([FromQuery] ReporteVentasFiltroDto filtro, CancellationToken cancellationToken = default) =>
        ObtenerAsync(filtro, RentabilidadAgrupacion.Categoria, cancellationToken);

    private async Task<IActionResult> ObtenerAsync(
        ReporteVentasFiltroDto filtro,
        RentabilidadAgrupacion agrupacion,
        CancellationToken cancellationToken)
    {
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(string.Join(" ", errores)));

        await RegistrarConsultaAsync(agrupacion);

        var resultado = await _service.ObtenerAsync(filtro, agrupacion, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ReporteRentabilidadDto>>.Ok(resultado));
    }

    private async Task RegistrarConsultaAsync(RentabilidadAgrupacion agrupacion)
    {
        if (_auditoria is null)
            return;

        var correlationId = HttpContext.TraceIdentifier;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Ver,
            $"Consulta de rentabilidad de ventas por '{agrupacion}'.",
            entidad: "RentabilidadVentas",
            valoresNuevos: new
            {
                CorrelationId = correlationId,
                Agrupacion = agrupacion.ToString()
            });
    }
}
