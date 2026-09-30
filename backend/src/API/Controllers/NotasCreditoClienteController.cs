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
[Route("notas-credito-cliente")]
public sealed class NotasCreditoClienteController : ControllerBase
{
    private readonly INotaCreditoClienteService _service;

    public NotasCreditoClienteController(INotaCreditoClienteService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var nota = await _service.GetByIdAsync(id);
        return nota is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Nota de crédito de cliente no encontrada",
                detail: "No existe una nota de crédito de cliente con el identificador indicado.")
            : Ok(ApiResponse<NotaCreditoClienteDto>.Ok(nota));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateNotaCreditoClienteDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<NotaCreditoClienteDto>.Ok(creada, "Nota de crédito de cliente creada correctamente."));
    }
}
