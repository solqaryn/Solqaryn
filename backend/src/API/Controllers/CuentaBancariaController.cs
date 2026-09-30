using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Application.Bancos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("cuentas-bancarias")]
public class CuentaBancariaController : ControllerBase
{
    private const string CuentaNoEncontradaType = "https://solqaryn.local/problems/cuenta-bancaria-no-encontrada";
    private readonly ICuentaBancariaService _service;

    public CuentaBancariaController(ICuentaBancariaService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll([FromQuery] CuentaBancariaQueryFilter filter)
    {
        var result = await _service.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("activas")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetActivas()
    {
        var result = await _service.GetActivasAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return CuentaNoEncontrada(id);

        return Ok(result);
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateCuentaBancariaDto dto)
    {
        var result = await _service.AddAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCuentaBancariaDto dto)
    {
        try
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return CuentaNoEncontrada(id);
        }
    }

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Activar)]
    public async Task<IActionResult> Activar(int id)
    {
        try
        {
            await _service.ActivarAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return CuentaNoEncontrada(id);
        }
    }

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Desactivar)]
    public async Task<IActionResult> Desactivar(int id)
    {
        try
        {
            await _service.DesactivarAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return CuentaNoEncontrada(id);
        }
    }

    private ObjectResult CuentaNoEncontrada(int id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Cuenta bancaria no encontrada",
        detail: $"No existe una cuenta bancaria con Id {id}.",
        type: CuentaNoEncontradaType);
}
