using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("asientos-contables")]
public sealed class AsientosContablesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAsientoContableWriter _writer;

    public AsientosContablesController(AppDbContext db, IAuditoriaService auditoria)
    {
        _db = db;
        _writer = new AsientoContableWriter(db, auditoria);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? numero,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 50,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 200);

        var query = _db.AsientosContables
            .AsNoTracking()
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .AsQueryable();

        if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(a => a.Fecha <= hasta.Value);
        if (!string.IsNullOrWhiteSpace(numero))
        {
            var normalized = numero.Trim();
            query = query.Where(a => a.Numero == normalized);
        }

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            pagina,
            tamano,
            total,
            items = entities.Select(AsientoContableApplicationService.Mapear).ToList()
        }));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.AsientosContables
            .AsNoTracking()
            .Include(a => a.Detalles)
                .ThenInclude(d => d.CuentaContable)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity is null
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Asiento contable no encontrado",
                detail: $"No existe un asiento contable con Id {id}.")
            : Ok(ApiResponse<AsientoContableDto>.Ok(AsientoContableApplicationService.Mapear(entity)));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Finanzas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create(
        [FromBody] CrearAsientoContableDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _writer.CreateAsync(dto, cancellationToken);

        if (!result.Created)
            return Ok(ApiResponse<AsientoContableDto>.Ok(result.Asiento));

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<AsientoContableDto>.Ok(result.Asiento));
    }
}
