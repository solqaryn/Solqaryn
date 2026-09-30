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
[Route("transferencias-inventario")]
public sealed class TransferenciasInventarioController : ControllerBase
{
    private readonly ITransferenciaInventarioService _service;
    private readonly ITransferenciaInventarioMovimientoService _movimientos;
    private readonly IAuditoriaService? _auditoria;

    public TransferenciasInventarioController(
        ITransferenciaInventarioService service,
        ITransferenciaInventarioMovimientoService movimientos,
        IAuditoriaService? auditoria = null)
    {
        _service = service;
        _movimientos = movimientos;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> Buscar([FromQuery] TransferenciaInventarioFiltroDto filtro)
    {
        var pagina = await _service.GetPagedAsync(filtro);
        return Ok(ApiResponse<PagedResult<TransferenciaInventarioDto>>.Ok(pagina));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var transferencia = await _service.GetByIdAsync(id);
        if (transferencia is null)
            return NotFound(ApiResponse<object>.Fail("Transferencia de inventario no encontrada."));

        return Ok(ApiResponse<TransferenciaInventarioDto>.Ok(transferencia));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateTransferenciaInventarioDto dto)
    {
        var creada = await _service.CreateAsync(dto);
        await AuditarAsync(
            AccionPermiso.Crear,
            creada.Id,
            $"Transferencia de inventario creada: {creada.Numero}",
            valoresNuevos: new { creada.Numero, creada.AlmacenOrigenId, creada.AlmacenDestinoId, creada.Estado });

        return CreatedAtAction(
            nameof(GetById),
            new { id = creada.Id },
            ApiResponse<TransferenciaInventarioDto>.Ok(creada, "Transferencia creada correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransferenciaInventarioDto dto)
    {
        var actualizada = await _service.UpdateAsync(id, dto);
        if (actualizada is null)
            return NotFound(ApiResponse<object>.Fail("Transferencia de inventario no encontrada."));

        await AuditarAsync(
            AccionPermiso.Editar,
            actualizada.Id,
            $"Transferencia de inventario actualizada: {actualizada.Numero}",
            valoresNuevos: new { actualizada.AlmacenOrigenId, actualizada.AlmacenDestinoId, actualizada.Estado });

        return Ok(ApiResponse<TransferenciaInventarioDto>.Ok(actualizada, "Transferencia actualizada correctamente."));
    }

    [HttpPost("{id:int}/solicitar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.CambiarEstado)]
    public async Task<IActionResult> Solicitar(int id)
    {
        var transferencia = await _service.SolicitarAsync(id);
        if (transferencia is null)
            return NotFound(ApiResponse<object>.Fail("Transferencia de inventario no encontrada."));

        await AuditarAsync(
            AccionPermiso.CambiarEstado,
            transferencia.Id,
            $"Transferencia solicitada: {transferencia.Numero}",
            valoresNuevos: new { transferencia.Estado });

        return Ok(ApiResponse<TransferenciaInventarioDto>.Ok(transferencia, "Transferencia solicitada correctamente."));
    }

    [HttpPost("{id:int}/aprobar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Aprobar)]
    public async Task<IActionResult> Aprobar(int id, [FromBody] AprobarTransferenciaInventarioDto dto)
    {
        var transferencia = await _service.AprobarAsync(id, dto);
        if (transferencia is null)
            return NotFound(ApiResponse<object>.Fail("Transferencia de inventario no encontrada."));

        await AuditarAsync(
            AccionPermiso.Aprobar,
            transferencia.Id,
            $"Transferencia aprobada: {transferencia.Numero}",
            valoresNuevos: new { transferencia.Estado });

        return Ok(ApiResponse<TransferenciaInventarioDto>.Ok(transferencia, "Transferencia aprobada correctamente."));
    }

    [HttpPost("{id:int}/despachar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Despachar(int id, [FromBody] DespacharTransferenciaInventarioDto dto)
    {
        var transferencia = await _movimientos.DespacharAsync(id, dto);
        if (transferencia is null)
            return NotFound(ApiResponse<object>.Fail("Transferencia de inventario no encontrada."));

        await AuditarAsync(
            AccionPermiso.Confirmar,
            transferencia.Id,
            $"Transferencia despachada: {transferencia.Numero}",
            valoresNuevos: new { transferencia.Estado });

        return Ok(ApiResponse<TransferenciaInventarioDto>.Ok(transferencia, "Transferencia despachada correctamente."));
    }

    [HttpPost("{id:int}/recibir")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Recibir(int id, [FromBody] RecibirTransferenciaInventarioDto dto)
    {
        var transferencia = await _movimientos.RecibirAsync(id, dto);
        if (transferencia is null)
            return NotFound(ApiResponse<object>.Fail("Transferencia de inventario no encontrada."));

        await AuditarAsync(
            AccionPermiso.Confirmar,
            transferencia.Id,
            $"Transferencia recibida: {transferencia.Numero}",
            valoresNuevos: new { transferencia.Estado });

        return Ok(ApiResponse<TransferenciaInventarioDto>.Ok(transferencia, "Transferencia recibida correctamente."));
    }

    [HttpPost("{id:int}/cancelar")]
    [RequierePermiso(ModuloSistema.MovimientosInventario, AccionPermiso.Anular)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarTransferenciaInventarioDto dto)
    {
        var transferencia = await _movimientos.CancelarAsync(id, dto);
        if (transferencia is null)
            return NotFound(ApiResponse<object>.Fail("Transferencia de inventario no encontrada."));

        await AuditarAsync(
            AccionPermiso.Anular,
            transferencia.Id,
            $"Transferencia cancelada: {transferencia.Numero}",
            valoresNuevos: new { transferencia.Estado },
            motivo: dto.Motivo);

        return Ok(ApiResponse<TransferenciaInventarioDto>.Ok(transferencia, "Transferencia cancelada correctamente."));
    }

    private Task AuditarAsync(
        AccionPermiso accion,
        int referenciaId,
        string descripcion,
        object? valoresNuevos = null,
        string? motivo = null)
    {
        if (_auditoria is null)
            return Task.CompletedTask;

        return _auditoria.RegistrarAsync(
            ModuloSistema.MovimientosInventario,
            accion,
            descripcion,
            referenciaId,
            entidad: nameof(TransferenciaInventario),
            valoresNuevos: valoresNuevos,
            motivo: motivo);
    }
}
