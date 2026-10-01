using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public interface IOutboxEffectDispatcher
{
    /// <summary>
    /// Ejecuta el efecto externo usando la identidad durable del mensaje. La
    /// implementación concreta debe propagar ClaveIdempotencia al proveedor.
    /// </summary>
    Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default);
}

public sealed record OutboxRetryProcessorOptions(
    int BatchSize = 50,
    int MaxAttempts = OutboxRetryPolicy.MaximoIntentosPredeterminado,
    TimeSpan? BaseDelay = null,
    TimeSpan? MaxDelay = null,
    TimeSpan? StaleClaimAfter = null,
    double JitterRatio = 0.20)
{
    public OutboxRetryProcessorOptions Validar()
    {
        var demoraBase = BaseDelay ?? OutboxRetryPolicy.DemoraBasePredeterminada;
        var demoraMaxima = MaxDelay ?? OutboxRetryPolicy.DemoraMaximaPredeterminada;
        var stale = StaleClaimAfter ?? TimeSpan.FromMinutes(10);

        if (BatchSize is <= 0 or > 200)
            throw new InvalidOperationException("Outbox:BatchSize debe estar entre 1 y 200.");
        if (MaxAttempts is <= 0 or > OutboxRetryPolicy.MaximoIntentosPermitido)
            throw new InvalidOperationException($"Outbox:MaxAttempts debe estar entre 1 y {OutboxRetryPolicy.MaximoIntentosPermitido}.");
        if (demoraBase <= TimeSpan.Zero)
            throw new InvalidOperationException("Outbox:BaseDelay debe ser mayor que cero.");
        if (demoraMaxima < demoraBase || demoraMaxima > OutboxRetryPolicy.DemoraMaximaPermitida)
            throw new InvalidOperationException("Outbox:MaxDelay debe ser mayor o igual que BaseDelay y no exceder siete días.");
        if (stale <= TimeSpan.Zero)
            throw new InvalidOperationException("Outbox:StaleClaimAfter debe ser mayor que cero.");
        if (JitterRatio is < 0 or > 1)
            throw new InvalidOperationException("Outbox:JitterRatio debe estar entre 0 y 1.");

        return this with
        {
            BaseDelay = demoraBase,
            MaxDelay = demoraMaxima,
            StaleClaimAfter = stale
        };
    }
}

public sealed record OutboxRetryBatchResult(
    int StaleRecuperados,
    int Reclamados,
    int Entregados,
    int Reprogramados,
    int DeadLetter,
    int Supersedidos);

/// <summary>
/// Procesa un lote tenant-bound. No contiene scheduler/HostedService: la etapa
/// de ejecución programada puede invocarlo sin acoplar Application a un proveedor
/// externo. Cada transición queda protegida por empresa + id + intento esperado.
/// </summary>
public sealed class OutboxRetryProcessor
{
    private const string AuditEntidad = "MensajeOutbox";

    private readonly IMensajeOutboxRepository _repository;
    private readonly OutboxRetryProcessorOptions _options;
    private readonly OutboxRetryPolicy _policy;
    private readonly Func<double> _jitterSample;
    private readonly IAuditoriaService? _auditoria;

    public OutboxRetryProcessor(
        IMensajeOutboxRepository repository,
        OutboxRetryProcessorOptions options,
        Func<double>? jitterSample = null,
        IAuditoriaService? auditoria = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Validar();
        _policy = new OutboxRetryPolicy(
            _options.MaxAttempts,
            _options.BaseDelay,
            _options.MaxDelay,
            _options.JitterRatio);
        _jitterSample = jitterSample ?? Random.Shared.NextDouble;
        _auditoria = auditoria;
    }

    public async Task<OutboxRetryBatchResult> ProcesarLoteAsync(
        int empresaId,
        DateTime ahoraUtc,
        IOutboxEffectDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(ahoraUtc));
        ArgumentNullException.ThrowIfNull(dispatcher);
        cancellationToken.ThrowIfCancellationRequested();

        var staleAntes = ahoraUtc - _options.StaleClaimAfter!.Value;
        var staleRecuperados = await _repository.RecuperarProcesandoStaleAsync(
            empresaId,
            staleAntes,
            ahoraUtc,
            _options.BatchSize,
            cancellationToken);

        var mensajes = await _repository.ClaimDisponiblesAsync(
            empresaId,
            ahoraUtc,
            _options.BatchSize,
            cancellationToken);

        var entregados = 0;
        var reprogramados = 0;
        var deadLetter = 0;
        var supersedidos = 0;

        foreach (var mensaje in mensajes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await dispatcher.DispatchAsync(mensaje, cancellationToken);
                var confirmado = await _repository.MarcarEntregadoAsync(
                    empresaId,
                    mensaje.Id,
                    mensaje.Intentos,
                    ahoraUtc,
                    cancellationToken);
                if (confirmado)
                {
                    entregados++;
                    await AuditarSeguroAsync(
                        "OUTBOX_RETRY_DELIVERED",
                        empresaId,
                        mensaje,
                        "DELIVERED",
                        cancellationToken);
                }
                else
                {
                    supersedidos++;
                    await AuditarSeguroAsync(
                        "OUTBOX_RETRY_SUPERSEDED",
                        empresaId,
                        mensaje,
                        "CLAIM_SUPERSEDED",
                        cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // No persistimos Exception.Message, payload ni datos del proveedor.
                // La auditoria SEC_AUDIT usa exclusivamente campos allow-listed.
                var decision = _policy.EvaluarDespuesDeFallo(
                    mensaje.Intentos,
                    ahoraUtc,
                    _jitterSample());
                var confirmado = await _repository.RegistrarFalloAsync(
                    empresaId,
                    mensaje.Id,
                    mensaje.Intentos,
                    "DELIVERY_FAILED",
                    ahoraUtc,
                    decision.DisponibleDesdeUtc ?? ahoraUtc,
                    decision.DeadLetter,
                    cancellationToken);

                if (!confirmado)
                {
                    supersedidos++;
                    await AuditarSeguroAsync(
                        "OUTBOX_RETRY_SUPERSEDED",
                        empresaId,
                        mensaje,
                        "CLAIM_SUPERSEDED",
                        cancellationToken);
                    continue;
                }

                if (decision.DeadLetter)
                {
                    deadLetter++;
                    await AuditarSeguroAsync(
                        "OUTBOX_RETRY_DEAD_LETTER",
                        empresaId,
                        mensaje,
                        "DEAD_LETTER",
                        cancellationToken);
                }
                else
                {
                    reprogramados++;
                    await AuditarSeguroAsync(
                        "OUTBOX_RETRY_RESCHEDULED",
                        empresaId,
                        mensaje,
                        "DELIVERY_FAILED",
                        cancellationToken);
                }
            }
        }

        return new OutboxRetryBatchResult(
            staleRecuperados,
            mensajes.Count,
            entregados,
            reprogramados,
            deadLetter,
            supersedidos);
    }

    private async Task AuditarSeguroAsync(
        string accion,
        int empresaId,
        MensajeOutbox mensaje,
        string resultado,
        CancellationToken cancellationToken)
    {
        if (_auditoria is null)
            return;

        // SEC_AUDIT: allow-list estricta. Nunca payload, ClaveIdempotencia,
        // Exception.Message ni datos del proveedor en la evidencia de auditoria.
        var detalle = new
        {
            // Se conservan los aliases historicos y se agregan los nombres
            // canonicos exigidos por la evidencia N7.3 para reconstruccion.
            EmpresaId = empresaId,
            MensajeId = mensaje.Id,
            TenantId = empresaId,
            OutboxId = mensaje.Id,
            CorrelationId = mensaje.CorrelationId,
            Intento = mensaje.Intentos,
            Resultado = resultado,
            Error = resultado is "DEAD_LETTER" or "DELIVERY_FAILED"
                ? "DELIVERY_FAILED"
                : null
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _auditoria.RegistrarAsync(
                ModuloSistema.Auditoria,
                AccionPermiso.Actualizar,
                accion,
                mensaje.Id,
                AuditEntidad,
                valoresNuevos: detalle,
                resultado: resultado);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // La observabilidad no puede reabrir una entrega ya confirmada ni
            // convertir un audit outage en un reenvio duplicado.
        }
    }
}
