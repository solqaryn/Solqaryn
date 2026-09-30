using System.Text.Json;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N72FOutboxSecurityAuditTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 11, 45, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Reintento_Fallido_Audita_Solo_Detalle_AllowListed()
    {
        var mensaje = CrearClaimed("{\"secret\":\"payload-sensitive\"}", "idem-sensitive");
        var repo = new FakeRepository(mensaje);
        var auditoria = new CapturingAuditoria();
        var processor = new OutboxRetryProcessor(
            repo,
            new OutboxRetryProcessorOptions(JitterRatio: 0),
            () => 0.5,
            auditoria);

        var result = await processor.ProcesarLoteAsync(
            7,
            AhoraUtc,
            new ThrowingDispatcher("provider-secret-message"));

        Assert.Equal(1, result.Reprogramados);
        Assert.Equal("OUTBOX_RETRY_RESCHEDULED", auditoria.Accion);
        Assert.Equal("MensajeOutbox", auditoria.Entidad);
        Assert.Contains("DELIVERY_FAILED", auditoria.DetalleJson, StringComparison.Ordinal);
        Assert.DoesNotContain("payload-sensitive", auditoria.DetalleJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("idem-sensitive", auditoria.DetalleJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider-secret", auditoria.DetalleJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Fallo_De_Auditoria_No_Reabre_Una_Entrega_Confirmada()
    {
        var mensaje = CrearClaimed("{\"safe\":true}", "idem-1");
        var repo = new FakeRepository(mensaje);
        var processor = new OutboxRetryProcessor(
            repo,
            new OutboxRetryProcessorOptions(),
            () => 0.5,
            new ThrowingAuditoria());

        var result = await processor.ProcesarLoteAsync(7, AhoraUtc, new SuccessDispatcher());

        Assert.Equal(1, result.Entregados);
        Assert.Equal(0, result.Reprogramados);
        Assert.Equal(0, result.DeadLetter);
        Assert.Equal(new[] { "recover", "claim", "delivered" }, repo.Calls);
    }

    private static MensajeOutbox CrearClaimed(string payload, string idempotencyKey)
    {
        var mensaje = MensajeOutbox.Crear(
            7,
            "inventory.changed",
            payload,
            idempotencyKey,
            creadoEnUtc: AhoraUtc.AddMinutes(-1));
        mensaje.MarcarProcesando(AhoraUtc);
        return mensaje;
    }

    private sealed class SuccessDispatcher : IOutboxEffectDispatcher
    {
        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingDispatcher : IOutboxEffectDispatcher
    {
        private readonly string _message;
        public ThrowingDispatcher(string message) => _message = message;
        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException(_message));
    }

    private sealed class CapturingAuditoria : IAuditoriaService
    {
        public string? Accion { get; private set; }
        public string? Entidad { get; private set; }
        public string DetalleJson { get; private set; } = string.Empty;

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
            Accion = descripcion;
            Entidad = entidad;
            DetalleJson = JsonSerializer.Serialize(valoresNuevos);
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
            string? error = null) =>
            RegistrarAsync(modulo, accion, descripcion, referenciaId, entidad, valoresAnteriores, valoresNuevos, motivo, resultado, error);

        public Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            Task.FromException<PagedResult<RegistroAuditoriaDto>>(new NotSupportedException());
    }

    private sealed class ThrowingAuditoria : IAuditoriaService
    {
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
            string? error = null) =>
            Task.FromException(new InvalidOperationException("audit-store-down"));

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
            string? error = null) =>
            Task.FromException(new InvalidOperationException("audit-store-down"));

        public Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            Task.FromException<PagedResult<RegistroAuditoriaDto>>(new InvalidOperationException("audit-store-down"));
    }

    private sealed class FakeRepository : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _claimed;
        public FakeRepository(params MensajeOutbox[] claimed) => _claimed = claimed;

        public List<string> Calls { get; } = new();

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<MensajeOutbox?> GetByEventoIdAsync(int empresaId, Guid eventoId, CancellationToken cancellationToken = default) => Task.FromResult<MensajeOutbox?>(null);
        public Task<bool> ExisteClaveIdempotenciaAsync(int empresaId, string claveIdempotencia, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(int empresaId, DateTime ahoraUtc, int maximoMensajes, CancellationToken cancellationToken = default)
        {
            Calls.Add("claim");
            return Task.FromResult(_claimed);
        }

        public Task<int> RecuperarProcesandoStaleAsync(int empresaId, DateTime staleAntesUtc, DateTime ahoraUtc, int maximoMensajes, CancellationToken cancellationToken = default)
        {
            Calls.Add("recover");
            return Task.FromResult(0);
        }

        public Task<bool> MarcarEntregadoAsync(int empresaId, int mensajeId, int intentoEsperado, DateTime ahoraUtc, CancellationToken cancellationToken = default)
        {
            Calls.Add("delivered");
            return Task.FromResult(true);
        }

        public Task<bool> RegistrarFalloAsync(int empresaId, int mensajeId, int intentoEsperado, string errorSeguro, DateTime ahoraUtc, DateTime disponibleDesdeUtc, bool deadLetter, CancellationToken cancellationToken = default)
        {
            Calls.Add("failed");
            return Task.FromResult(true);
        }
    }
}
