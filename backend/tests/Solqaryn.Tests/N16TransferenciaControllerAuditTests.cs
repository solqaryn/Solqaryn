using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaControllerAuditTests
{
    [Fact]
    public async Task Cancelar_RegistraAuditoriaConMotivoYReferencia()
    {
        var movimientos = new MovimientoServiceFake();
        var auditoria = new AuditoriaServiceFake();
        var controller = new TransferenciasInventarioController(
            new TransferenciaServiceFake(),
            movimientos,
            auditoria);
        var request = new CancelarTransferenciaInventarioDto { Motivo = "Daño durante tránsito" };

        var result = await controller.Cancelar(77, request);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(77, movimientos.UltimoIdCancelado);
        Assert.NotNull(auditoria.Ultimo);
        Assert.Equal(ModuloSistema.MovimientosInventario, auditoria.Ultimo!.Modulo);
        Assert.Equal(AccionPermiso.Anular, auditoria.Ultimo.Accion);
        Assert.Equal(77, auditoria.Ultimo.ReferenciaId);
        Assert.Equal("Daño durante tránsito", auditoria.Ultimo.Motivo);
        Assert.Equal("TransferenciaInventario", auditoria.Ultimo.Entidad);
    }

    private sealed class MovimientoServiceFake : ITransferenciaInventarioMovimientoService
    {
        public int? UltimoIdCancelado { get; private set; }

        public Task<TransferenciaInventarioDto?> DespacharAsync(int id, DespacharTransferenciaInventarioDto dto) =>
            throw new NotSupportedException();

        public Task<TransferenciaInventarioDto?> RecibirAsync(int id, RecibirTransferenciaInventarioDto dto) =>
            throw new NotSupportedException();

        public Task<TransferenciaInventarioDto?> CancelarAsync(int id, CancelarTransferenciaInventarioDto dto)
        {
            UltimoIdCancelado = id;
            return Task.FromResult<TransferenciaInventarioDto?>(new TransferenciaInventarioDto
            {
                Id = id,
                Numero = "TRF-77",
                Estado = EstadoTransferenciaInventario.Cancelada.ToString()
            });
        }
    }

    private sealed class TransferenciaServiceFake : ITransferenciaInventarioService
    {
        public Task<PagedResult<TransferenciaInventarioDto>> GetPagedAsync(TransferenciaInventarioFiltroDto filtro) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto> CreateAsync(CreateTransferenciaInventarioDto dto) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto?> UpdateAsync(int id, UpdateTransferenciaInventarioDto dto) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto?> SolicitarAsync(int id) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto?> AprobarAsync(int id, AprobarTransferenciaInventarioDto dto) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto?> DespacharAsync(int id, DespacharTransferenciaInventarioDto dto) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto?> RecibirAsync(int id, RecibirTransferenciaInventarioDto dto) => throw new NotSupportedException();
        public Task<TransferenciaInventarioDto?> CancelarAsync(int id, CancelarTransferenciaInventarioDto dto) => throw new NotSupportedException();
    }

    private sealed class AuditoriaServiceFake : IAuditoriaService
    {
        public Registro? Ultimo { get; private set; }

        public Task RegistrarAsync(
            ModuloSistema modulo,
            AccionPermiso accion,
            string descripcion,
            int? referenciaId = null,
            string? entidad = null,
            object? valoresAnteriores = null,
            object? valoresNuevos = null,
            string? motivo = null,
            string resultado = "Exito",
            string? error = null)
        {
            Ultimo = new Registro(modulo, accion, referenciaId, entidad, motivo);
            return Task.CompletedTask;
        }

        public Task RegistrarEstrictoAsync(
            ModuloSistema modulo,
            AccionPermiso accion,
            string descripcion,
            int? referenciaId = null,
            string? entidad = null,
            object? valoresAnteriores = null,
            object? valoresNuevos = null,
            string? motivo = null,
            string resultado = "Exito",
            string? error = null) => throw new NotSupportedException();

        public Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            throw new NotSupportedException();
    }

    private sealed record Registro(
        ModuloSistema Modulo,
        AccionPermiso Accion,
        int? ReferenciaId,
        string? Entidad,
        string? Motivo);
}
