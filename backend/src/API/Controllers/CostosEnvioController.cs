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
[Route("costos-envio")]
public sealed class CostosEnvioController(ICostoEnvioService service, IAuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll() => Ok(ApiResponse<List<CostoEnvioDto>>.Ok(await service.GetAllAsync()));

    // Dedicated management read contract. An older deployed backend must not
    // be used as a fallback by the administrative screen.
    [HttpGet("administracion")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> GetAdministracion() => Ok(ApiResponse<List<CostoEnvioDto>>.Ok(await service.GetAllAsync()));

    [HttpGet("predeterminado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPredeterminado() =>
        await service.GetPredeterminadoVigenteAsync() is { } item
            ? Ok(ApiResponse<CostoEnvioDto>.Ok(item)) : NotFound(ApiResponse<object>.Fail("No existe un costo de envío predeterminado vigente."));

    [HttpPost("resolver")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> Resolver([FromBody] ResolverCostoEnvioDto dto) =>
        await service.ResolverAsync(dto) is { } item
            ? Ok(ApiResponse<CostoEnvioDto>.Ok(item)) : NotFound(ApiResponse<object>.Fail("No existe un costo de envío vigente aplicable."));

    [HttpGet("{id:int}/historial")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Historial(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!await service.ExisteIncluidoEliminadoAsync(id))
            return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
        var result = await auditoria.GetFilteredAsync(new AuditoriaFiltroDto { Modulo = "Facturacion", Entidad = "CostoEnvio",
            ReferenciaId = id, Page = Math.Max(1, page), PageSize = Math.Clamp(pageSize, 1, 100) });
        return Ok(ApiResponse<PagedResult<RegistroAuditoriaDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id) => await service.GetByIdAsync(id) is { } item
        ? Ok(ApiResponse<CostoEnvioDto>.Ok(item)) : NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));

    [HttpPost]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Create([FromBody] GuardarCostoEnvioDto dto)
    {
        var item = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ApiResponse<CostoEnvioDto>.Ok(item));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Update(int id, [FromBody] GuardarCostoEnvioDto dto) =>
        await service.UpdateAsync(id, dto) is { } item
            ? Ok(ApiResponse<CostoEnvioDto>.Ok(item)) : NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoCostoEnvioDto dto) =>
        await service.CambiarEstadoAsync(id, dto.Activo)
            ? Ok(ApiResponse<object>.Ok(new { id, dto.Activo })) : NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Delete(int id) => await service.EliminarAsync(id)
        ? Ok(ApiResponse<object>.Ok(new { id })) : NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
}
public sealed class CambiarEstadoCostoEnvioDto { public bool Activo { get; set; } }
