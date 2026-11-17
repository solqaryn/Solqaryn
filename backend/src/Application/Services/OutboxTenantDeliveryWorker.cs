using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

/// <summary>
/// Gate implementado por una futura integración server-side con consentimiento y
/// habilitación de canal. Sin implementación o registro DI permisivo por defecto.
/// No recibe payload, Idempotency-Key, destinatarios ni secretos.
/// </summary>
public interface IOutboxTenantDeliveryGate
{
    Task<bool> AutorizarTenantAsync(int empresaId, CancellationToken cancellationToken = default);
    Task<bool> AutorizarMensajeAsync(
        int empresaId, Guid eventoId, string tipoEvento, CancellationToken cancellationToken = default);
}

/// <summary>
/// Worker acotado invocable explícitamente; no crea HostedService, scheduler ni
/// conexiones a proveedores. Los CAS/retries quedan en OutboxRetryProcessor.
/// </summary>
public sealed class OutboxTenantDeliveryWorker
{
    private readonly OutboxRetryProcessor _processor;
    private readonly IOutboxTenantDeliveryGate _gate;
    private readonly IOutboxEffectDispatcher _dispatcher;

    public OutboxTenantDeliveryWorker(
        OutboxRetryProcessor processor, IOutboxTenantDeliveryGate gate, IOutboxEffectDispatcher dispatcher)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public async Task<OutboxRetryBatchResult> ProcesarAsync(
        int empresaId, DateTime ahoraUtc, CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(ahoraUtc));
        cancellationToken.ThrowIfCancellationRequested();
        // Denegar ANTES de recuperar stale, reclamar el lote o escribir en DB.
        if (!await _gate.AutorizarTenantAsync(empresaId, cancellationToken))
            throw new UnauthorizedAccessException("Empresa sin autorización de entrega outbox.");
        return await _processor.ProcesarLoteAsync(
            empresaId, ahoraUtc, new DispatcherVerificado(empresaId, _gate, _dispatcher), cancellationToken);
    }

    private sealed class DispatcherVerificado : IOutboxEffectDispatcher
    {
        private readonly int _empresaId;
        private readonly IOutboxTenantDeliveryGate _gate;
        private readonly IOutboxEffectDispatcher _inner;
        public DispatcherVerificado(int empresaId, IOutboxTenantDeliveryGate gate, IOutboxEffectDispatcher inner)
        {
            _empresaId = empresaId;
            _gate = gate;
            _inner = inner;
        }

        public async Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (mensaje is null || mensaje.EmpresaId != _empresaId ||
                mensaje.EventoId == Guid.Empty || string.IsNullOrWhiteSpace(mensaje.TipoEvento) ||
                string.IsNullOrWhiteSpace(mensaje.ClaveIdempotencia))
                throw new InvalidOperationException("Claim outbox sin identidad/tenant válido.");
            if (!await _gate.AutorizarMensajeAsync(
                _empresaId, mensaje.EventoId, mensaje.TipoEvento, cancellationToken))
                throw new UnauthorizedAccessException("Mensaje sin autorización vigente.");
            await _inner.DispatchAsync(mensaje, cancellationToken);
        }
    }
}
