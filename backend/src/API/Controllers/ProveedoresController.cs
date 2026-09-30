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
[Route("proveedores")]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _service;

    public ProveedoresController(IProveedorService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var proveedores = await _service.GetAllAsync();
        return Ok(ApiResponse<List<ProveedorDto>>.Ok(proveedores));
    }

    [HttpGet("activos")]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivos()
    {
        var proveedores = await _service.GetActivosAsync();
        return Ok(ApiResponse<List<ProveedorDto>>.Ok(proveedores));
    }

    [HttpGet("buscar")]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] string termino)
    {
        var proveedores = await _service.BuscarActivosAsync(termino);
        return Ok(ApiResponse<List<ProveedorDto>>.Ok(proveedores));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var proveedor = await _service.GetByIdAsync(id);
        if (proveedor is null) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<ProveedorDto>.Ok(proveedor));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateProveedorDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id },
            ApiResponse<ProveedorDto>.Ok(creado, "Proveedor creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProveedorDto dto)
    {
        var actualizado = await _service.UpdateAsync(id, dto);
        if (actualizado is null) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<ProveedorDto>.Ok(actualizado, "Proveedor actualizado correctamente."));
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        var proveedor = await _service.CambiarEstadoAsync(id, true);
        if (proveedor is null) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<ProveedorDto>.Ok(proveedor, "Proveedor activado correctamente."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var proveedor = await _service.CambiarEstadoAsync(id, false);
        if (proveedor is null) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<ProveedorDto>.Ok(proveedor, "Proveedor desactivado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Proveedores, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await _service.DeleteAsync(id);
        if (!eliminado) return NotFound(ApiResponse<object>.Fail("Proveedor no encontrado."));
        return Ok(ApiResponse<object>.Ok(new { }, "Proveedor eliminado correctamente."));
    }
}
