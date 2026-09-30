using System.Text.Json;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N73FOutboxAuditTrailTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 18, 5, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DeadLetter_Audit_Permite_Reconstruir_Identidad_Correlacion_Resultado_Y_Error_Seguro()
    {
        var mensaje = MensajeOutbox.Crear(
            7,
            "inventory.audit.certificate",
            "{\"secret\":\"must-not-leak\"}",
            "n73f-idempotency-must-not-leak",
            correlationId: "corr-n73f-001",
            creadoEnUtc: AhoraUtc.AddMinutes(-1));
        mensaje.MarcarProcesando(AhoraUtc);

        var repository = new ConfirmingDeadLetterRepository(mensaje);
        var auditoria = new CapturingAuditoriaService();
        var processor = new OutboxRetryProcessor(
            repository,
            new OutboxRetryProcessorOptions(MaxAttempts: 1, JitterRatio: 0),
            () => 0.5,
            auditoria);

        var result = await processor.ProcesarLoteAsync(
            7,
            AhoraUtc,
            new ThrowingDispatcher(),
            CancellationToken.None);

        Assert.Equal(1, result.DeadLetter);
        Assert.Equal("OUTBOX_RETRY_DEAD_LETTER", auditoria.Descripcion);
        Assert.Equal("DEAD_LETTER", auditoria.Resultado);

        var json = JsonSerializer.Serialize(auditoria.ValoresNuevos);
        using var document = JsonDocument.Parse(json);
        var detail = document.RootElement;

        Assert.Equal(7, detail.GetProperty("TenantId").GetInt32());
        Assert.Equal(mensaje.Id, detail.GetProperty("OutboxId").GetInt32());
        Assert.Equal("corr-n73f-001", detail.GetProperty("CorrelationId").GetString());
        Assert.Equal("DEAD_LETTER", detail.GetProperty("Resultado").GetString());
        Assert.Equal("DELIVERY_FAILED", detail.GetProperty("Error").GetString());
        Assert.DoesNotContain("must-not-leak", json, StringComparison.Ordinal);
    }

    private sealed class ThrowingDispatcher : IOutboxEffectDispatcher
    {
        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("provider-detail-must-not-leak"));
    }

    private sealed class ConfirmingDeadLetterRepository : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _claimed;

        public ConfirmingDeadLetterRepository(params MensajeOutbox[] claimed) => _claimed = claimed;

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<MensajeOutbox?> GetByEventoIdAsync(int empresaId, Guid eventoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MensajeOutbox?>(null);

        public Task<bool> ExisteClaveIdempotenciaAsync(int empresaId, string claveIdempotencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(int empresaId, DateTime ahoraUtc, int maximoMensajes, CancellationToken cancellationToken = default) =>
            Task.FromResult(_claimed);

        public Task<int> RecuperarProcesandoStaleAsync(int empresaId, DateTime staleAntesUtc, DateTime ahoraUtc, int maximoMensajes, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> MarcarEntregadoAsync(int empresaId, int mensajeId, int intentoEsperado, DateTime ahoraUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> RegistrarFalloAsync(
            int empresaId,
            int mensajeId,
            int intentoEsperado,
            string errorSeguro,
            DateTime ahoraUtc,
            DateTime disponibleDesdeUtc,
            bool deadLetter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class CapturingAuditoriaService : IAuditoriaService
    {
        public string? Descripcion { get; private set; }
        public object? ValoresNuevos { get; private set; }
        public string? Resultado { get; private set; }

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
            Descripcion = descripcion;
            ValoresNuevos = valoresNuevos;
            Resultado = resultado;
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
            string? error = null) => Task.CompletedTask;

        public Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro) =>
            throw new NotSupportedException();
    }
}
