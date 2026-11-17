using Solqaryn.Domain.Entities;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<OutboxTenantDeliveryWorker>? _logger;

    public OutboxTenantDeliveryWorker(
        OutboxRetryProcessor processor, IOutboxTenantDeliveryGate gate, IOutboxEffectDispatcher dispatcher,
        ILogger<OutboxTenantDeliveryWorker>? logger = null)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger;
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
        {
            _logger?.LogWarning(
                "OUTBOX_TENANT_DENIED EmpresaId={EmpresaId} Codigo={Codigo}",
                empresaId, "TENANT_DENIED");
            throw new UnauthorizedAccessException("Empresa sin autorización de entrega outbox.");
        }

        try
        {
            var resultado = await _processor.ProcesarLoteAsync(
                empresaId, ahoraUtc,
                new DispatcherVerificado(empresaId, _gate, _dispatcher, _logger),
                cancellationToken);
            // Métricas estructuradas sin payload, destinatarios, claves ni errores del proveedor.
            _logger?.LogInformation(
                "OUTBOX_BATCH_SUMMARY EmpresaId={EmpresaId} Reclamados={Reclamados} Entregados={Entregados} Reprogramados={Reprogramados} DeadLetter={DeadLetter} Supersedidos={Supersedidos}",
                empresaId, resultado.Reclamados, resultado.Entregados,
                resultado.Reprogramados, resultado.DeadLetter, resultado.Supersedidos);
            return resultado;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // No adjuntar Exception ni Exception.Message: pueden contener secretos del proveedor.
            _logger?.LogError("OUTBOX_PROCESS_FAILED EmpresaId={EmpresaId} Codigo={Codigo}",
                empresaId, "PROCESS_FAILED");
            throw;
        }
    }

    private sealed class DispatcherVerificado : IOutboxEffectDispatcher
    {
        private readonly int _empresaId;
        private readonly IOutboxTenantDeliveryGate _gate;
        private readonly IOutboxEffectDispatcher _inner;
        private readonly ILogger<OutboxTenantDeliveryWorker>? _logger;
        public DispatcherVerificado(int empresaId, IOutboxTenantDeliveryGate gate, IOutboxEffectDispatcher inner,
            ILogger<OutboxTenantDeliveryWorker>? logger)
        {
            _empresaId = empresaId;
            _gate = gate;
            _inner = inner;
            _logger = logger;
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
            {
                _logger?.LogWarning("OUTBOX_MESSAGE_DENIED EmpresaId={EmpresaId} Codigo={Codigo}",
                    _empresaId, "MESSAGE_DENIED");
                throw new UnauthorizedAccessException("Mensaje sin autorización vigente.");
            }
            await _inner.DispatchAsync(mensaje, cancellationToken);
        }
    }
}
