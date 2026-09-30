using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("conteos-inventario")]
public sealed class ConteosInventarioController : ControllerBase
{
    private readonly IConteoInventarioService _service;
    private readonly IAuditoriaService? _auditoria;

    public ConteosInventarioController(IConteoInventarioService service, IAuditoriaService? auditoria = null)
    {
        _service = service;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] ConteoInventarioQueryDto query)
    {
        var pagina = await _service.GetPagedAsync(query);
        return Ok(ApiResponse<PagedResult<ConteoInventarioDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var conteo = await _service.GetByIdAsync(id);
        return conteo is null
            ? NotFound(ApiResponse<object>.Fail("Conteo de inventario no encontrado."))
            : Ok(ApiResponse<ConteoInventarioDto>.Ok(conteo));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateConteoInventarioDto dto)
    {
        var creado = await _service.CreateAsync(dto);
        await AuditarAsync(AccionPermiso.Crear, creado, "Conteo físico creado.");
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, ApiResponse<ConteoInventarioDto>.Ok(creado, "Conteo creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConteoInventarioDto dto)
    {
        var conteo = await _service.UpdateAsync(id, dto);
        if (conteo is null) return NotFound(ApiResponse<object>.Fail("Conteo de inventario no encontrado."));
        await AuditarAsync(AccionPermiso.Editar, conteo, "Conteo físico actualizado.");
        return Ok(ApiResponse<ConteoInventarioDto>.Ok(conteo, "Conteo actualizado correctamente."));
    }

    [HttpPost("{id:int}/iniciar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.CambiarEstado)]
    public Task<IActionResult> Iniciar(int id) => EjecutarTransicionAsync(id, _service.IniciarAsync, AccionPermiso.CambiarEstado, "Conteo iniciado correctamente.");

    [HttpPut("{id:int}/detalles/{detalleId:int}/captura")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> Capturar(int id, int detalleId, [FromBody] CapturarConteoInventarioDetalleDto dto)
    {
        var conteo = await _service.CapturarDetalleAsync(id, detalleId, dto);
        if (conteo is null) return NotFound(ApiResponse<object>.Fail("Conteo de inventario no encontrado."));
        await AuditarCapturaAsync(conteo, detalleId, dto.CantidadContada);
        return Ok(ApiResponse<ConteoInventarioDto>.Ok(conteo, "Captura registrada correctamente."));
    }

    [HttpPut("{id:int}/detalles/captura-lote")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> CapturarLote(int id, [FromBody] CapturarConteoInventarioLoteDto dto)
    {
        var conteo = await _service.CapturarLoteAsync(id, dto);
        if (conteo is null) return NotFound(ApiResponse<object>.Fail("Conteo de inventario no encontrado."));
        await AuditarCapturaLoteAsync(conteo, dto);
        return Ok(ApiResponse<ConteoInventarioDto>.Ok(conteo, "Captura por lote registrada correctamente."));
    }

    [HttpPost("{id:int}/cerrar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Cerrar)]
    public Task<IActionResult> Cerrar(int id) => EjecutarTransicionAsync(id, _service.CerrarAsync, AccionPermiso.Cerrar, "Conteo cerrado correctamente.");

    [HttpPost("{id:int}/aprobar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Aprobar)]
    public Task<IActionResult> Aprobar(int id) => EjecutarTransicionAsync(id, _service.AprobarAsync, AccionPermiso.Aprobar, "Conteo aprobado correctamente.");

    [HttpPost("{id:int}/generar-ajuste")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Crear)]
    public async Task<IActionResult> GenerarAjuste(int id)
    {
        var ajuste = await _service.GenerarAjusteAsync(id);
        if (ajuste is null)
            return NotFound(ApiResponse<object>.Fail("Conteo de inventario no encontrado."));

        await AuditarGeneracionAjusteAsync(id, ajuste.Id);
        return Ok(ApiResponse<AjusteInventarioDto>.Ok(
            ajuste,
            "Ajuste borrador generado desde las diferencias del conteo. Requiere confirmación formal posterior."));
    }

    [HttpPost("{id:int}/cancelar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarConteoInventarioDto dto)
    {
        var conteo = await _service.CancelarAsync(id, dto.Motivo);
        if (conteo is null) return NotFound(ApiResponse<object>.Fail("Conteo de inventario no encontrado."));
        await AuditarAsync(AccionPermiso.Anular, conteo, "Conteo cancelado.", dto.Motivo);
        return Ok(ApiResponse<ConteoInventarioDto>.Ok(conteo, "Conteo cancelado correctamente."));
    }

    private async Task<IActionResult> EjecutarTransicionAsync(
        int id,
        Func<int, Task<ConteoInventarioDto?>> accion,
        AccionPermiso permiso,
        string mensaje)
    {
        var conteo = await accion(id);
        if (conteo is null) return NotFound(ApiResponse<object>.Fail("Conteo de inventario no encontrado."));
        await AuditarAsync(permiso, conteo, mensaje);
        return Ok(ApiResponse<ConteoInventarioDto>.Ok(conteo, mensaje));
    }

    private Task AuditarAsync(AccionPermiso accion, ConteoInventarioDto conteo, string descripcion, string? motivo = null)
    {
        if (_auditoria is null) return Task.CompletedTask;
        return _auditoria.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            accion,
            descripcion,
            conteo.Id,
            entidad: nameof(ConteoInventario),
            valoresNuevos: new { conteo.Numero, conteo.Estado, conteo.AlmacenId, conteo.UbicacionAlmacenId, conteo.CategoriaId },
            motivo: motivo);
    }

    private Task AuditarCapturaAsync(ConteoInventarioDto conteo, int detalleId, int cantidadContada)
    {
        if (_auditoria is null) return Task.CompletedTask;
        return _auditoria.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Editar,
            $"Línea {detalleId} capturada.",
            conteo.Id,
            entidad: nameof(ConteoInventario),
            valoresNuevos: new
            {
                conteo.Numero,
                ConteoInventarioDetalleId = detalleId,
                CantidadContada = cantidadContada,
                conteo.AlmacenId,
                conteo.UbicacionAlmacenId
            });
    }

    private Task AuditarCapturaLoteAsync(ConteoInventarioDto conteo, CapturarConteoInventarioLoteDto dto)
    {
        if (_auditoria is null) return Task.CompletedTask;
        return _auditoria.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Editar,
            $"Captura por lote registrada ({dto.Lineas.Count} líneas).",
            conteo.Id,
            entidad: nameof(ConteoInventario),
            valoresNuevos: new
            {
                conteo.Numero,
                conteo.AlmacenId,
                conteo.UbicacionAlmacenId,
                Lineas = dto.Lineas.Select(linea => new { linea.DetalleId, linea.CantidadContada }).ToArray()
            });
    }

    private Task AuditarGeneracionAjusteAsync(int conteoId, int ajusteInventarioId)
    {
        if (_auditoria is null) return Task.CompletedTask;
        return _auditoria.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Crear,
            "Ajuste borrador generado desde diferencias de conteo físico.",
            conteoId,
            entidad: nameof(ConteoInventario),
            valoresNuevos: new { ConteoInventarioId = conteoId, AjusteInventarioId = ajusteInventarioId });
    }
}
