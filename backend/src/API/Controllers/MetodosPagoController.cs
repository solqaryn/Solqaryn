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
[Route("metodos-pago")]
public sealed class MetodosPagoController : ControllerBase
{
    private readonly IMetodoPagoService _service;
    private readonly IFacturaRepository _facturaRepository;
    public MetodosPagoController(IMetodoPagoService service, IFacturaRepository facturaRepository){_service=service;_facturaRepository=facturaRepository;}
    [HttpGet]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()=>Ok(ApiResponse<List<MetodoPagoDto>>.Ok(await _service.GetAllAsync()));
    [HttpGet("activos")]
    public async Task<IActionResult> GetActivos()=>Ok(ApiResponse<List<MetodoPagoDto>>.Ok(await _service.GetActivosAsync()));
    [HttpGet("bancos-activos")]
    public async Task<IActionResult> GetBancosActivos(){var bancos=await _facturaRepository.GetBancosActivosAsync();var lookup=bancos.Select(b=>new {b.Id,b.Codigo,b.Nombre}).ToList();return Ok(ApiResponse<object>.Ok(lookup));}
    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id){var item=await _service.GetByIdAsync(id);return item is null?NotFound(ApiResponse<object>.Fail("Método de pago no encontrado.")):Ok(ApiResponse<MetodoPagoDto>.Ok(item));}
    [HttpPost]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateMetodoPagoDto dto){var creado=await _service.CreateAsync(dto);return CreatedAtAction(nameof(GetById),new{id=creado.Id},ApiResponse<MetodoPagoDto>.Ok(creado,"Método de pago creado correctamente."));}
    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id,[FromBody] UpdateMetodoPagoDto dto){var item=await _service.UpdateAsync(id,dto);return item is null?NotFound(ApiResponse<object>.Fail("Método de pago no encontrado.")):Ok(ApiResponse<MetodoPagoDto>.Ok(item,"Método de pago actualizado correctamente."));}
    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)=>await CambiarEstado(id,true);
    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)=>await CambiarEstado(id,false);
    [HttpPut("orden")]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.Editar)]
    public async Task<IActionResult> Reordenar([FromBody] List<ReordenarMetodoPagoDto> items){await _service.ReordenarAsync(items);return Ok(ApiResponse<object>.Ok(new{},"Orden de métodos de pago actualizado correctamente."));}
    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.MetodosPago, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id){var eliminado=await _service.DeleteAsync(id);return eliminado?Ok(ApiResponse<object>.Ok(new{},"Método de pago eliminado correctamente.")):NotFound(ApiResponse<object>.Fail("Método de pago no encontrado."));}
    private async Task<IActionResult> CambiarEstado(int id,bool activo){var item=await _service.CambiarEstadoAsync(id,activo);if(item is null)return NotFound(ApiResponse<object>.Fail("Método de pago no encontrado."));return Ok(ApiResponse<MetodoPagoDto>.Ok(item,activo?"Método de pago activado correctamente.":"Método de pago desactivado correctamente."));}
}