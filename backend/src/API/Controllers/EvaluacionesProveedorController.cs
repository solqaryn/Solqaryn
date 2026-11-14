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

[ApiController]
[Authorize]
[Route("evaluaciones-proveedor")]
public sealed class EvaluacionesProveedorController : ControllerBase
{
    private readonly IEvaluacionProveedorService _service;

    public EvaluacionesProveedorController(
        AppDbContext db,
        IRecepcionCompraRepository recepciones,
        IOrdenCompraRepository ordenes,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        ArgumentNullException.ThrowIfNull(db);
        _service = new EvaluacionProveedorService(
            new EvaluacionProveedorRepository(db),
            recepciones,
            ordenes,
            unitOfWork,
            auditoria);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] EvaluacionProveedorFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<EvaluacionProveedorDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var evaluacion = await _service.GetByIdAsync(id);
        return evaluacion is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Evaluación de proveedor no encontrada", detail: "No existe una evaluación con el identificador indicado.")
            : Ok(ApiResponse<EvaluacionProveedorDto>.Ok(evaluacion));
    }

    [HttpPost("recepciones/{recepcionCompraId:int}/generar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Generar(int recepcionCompraId)
    {
        var evaluacion = await _service.GenerarPorRecepcionAsync(recepcionCompraId);
        return CreatedAtAction(nameof(GetById), new { id = evaluacion.Id }, ApiResponse<EvaluacionProveedorDto>.Ok(evaluacion, "Evaluación de proveedor generada correctamente."));
    }
}
