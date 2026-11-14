using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("facturas-proveedor")]
public sealed class FacturasProveedorController : ControllerBase
{
    private readonly IFacturaProveedorService _service;
    private readonly ILogger<FacturasProveedorController> _logger;

    public FacturasProveedorController(IFacturaProveedorService service, ILogger<FacturasProveedorController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] FacturaProveedorFiltroDto filtro)
    {
        _logger.LogInformation("Buscando facturas de proveedor.");
        var pagina = await _service.GetPagedAsync(filtro);
        _logger.LogInformation("Búsqueda completada. {Count} facturas encontradas.", pagina.Items.Count);
        return Ok(ApiResponse<PagedResult<FacturaProveedorDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("Consultando factura de proveedor con ID {Id}.", id);
        var factura = await _service.GetByIdAsync(id);
        if (factura is null)
        {
            _logger.LogWarning("Factura de proveedor con ID {Id} no encontrada.", id);
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Factura de proveedor no encontrada", detail: "No existe una factura de proveedor con el identificador indicado.");
        }
        return Ok(ApiResponse<FacturaProveedorDto>.Ok(factura));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateFacturaProveedorDto dto)
    {
        _logger.LogInformation("Solicitando creación de factura de proveedor para orden {OrdenId}.", dto.OrdenCompraId);
        var creada = await _service.CreateAsync(dto);
        _logger.LogInformation("Factura de proveedor {Id} creada exitosamente.", creada.Id);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<FacturaProveedorDto>.Ok(creada, "Factura de proveedor creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFacturaProveedorDto dto)
    {
        _logger.LogInformation("Solicitando actualización de factura de proveedor {Id}.", id);
        var actualizada = await _service.UpdateAsync(id, dto);
        _logger.LogInformation("Factura de proveedor {Id} actualizada exitosamente.", id);
        return Ok(ApiResponse<FacturaProveedorDto>.Ok(actualizada, "Factura de proveedor actualizada correctamente."));
    }

    [HttpPost("{id:int}/registrar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Registrar(int id)
    {
        _logger.LogInformation("Solicitando registro de factura de proveedor {Id}.", id);
        var registrada = await _service.RegistrarAsync(id);
        _logger.LogInformation("Factura de proveedor {Id} registrada exitosamente.", id);
        return Ok(ApiResponse<FacturaProveedorDto>.Ok(registrada, "Factura de proveedor registrada correctamente."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularFacturaProveedorDto dto)
    {
        _logger.LogInformation("Solicitando anulación de factura de proveedor {Id}.", id);
        var anulada = await _service.AnularAsync(id, dto);
        _logger.LogInformation("Factura de proveedor {Id} anulada exitosamente.", id);
        return Ok(ApiResponse<FacturaProveedorDto>.Ok(anulada, "Factura de proveedor anulada correctamente."));
    }
}
