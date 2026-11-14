using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Proyección read-only de cuentas por cobrar sobre la autoridad vigente Factura/FacturaPago.
/// No crea ni mantiene un ledger CxC independiente.
/// </summary>
[ApiController]
[Authorize]
[Route("cuentas-por-cobrar")]
public class CuentasPorCobrarController : ControllerBase
{
    private readonly IFacturaService _facturaService;

    public CuentasPorCobrarController(IFacturaService facturaService)
    {
        _facturaService = facturaService;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var facturas = await _facturaService.GetAllAsync();
        var cuentas = facturas
            .Where(f =>
                f.SaldoPendiente > 0 &&
                !string.Equals(f.Estado, EstadoFactura.Anulada.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(f.Estado, EstadoFactura.Cancelada.ToString(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.FechaVencimiento ?? DateTime.MaxValue)
            .ThenBy(f => f.NumeroFactura)
            .ToList();

        return Ok(ApiResponse<List<FacturaDto>>.Ok(cuentas));
    }
}
