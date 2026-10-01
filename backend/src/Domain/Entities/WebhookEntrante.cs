using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Representa una entrega de webhook entrante cuya firma ya fue validada por
/// la capa de aplicación. El dominio conserva únicamente identidad durable,
/// huella del payload y trazabilidad; nunca la firma ni secretos del proveedor.
/// </summary>
public sealed class WebhookEntrante : BaseEntity
{
    public const int LongitudMaximaProveedor = 120;
    public const int LongitudMaximaEventoExternoId = 200;
    public const int LongitudMaximaTipoEvento = 160;
    public const int LongitudMaximaPayloadHash = 128;
    public const int LongitudMaximaCorrelationId = 160;
    public const int LongitudMaximaMotivoRechazo = 1000;

    private WebhookEntrante()
    {
    }

    private WebhookEntrante(
        int empresaId,
        string proveedor,
        string eventoExternoId,
        string tipoEvento,
        string payloadHash,
        string? correlationId,
        DateTime emitidoEnUtc,
        DateTime recibidoEnUtc)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa/tenant del webhook es obligatoria.");

        EmpresaId = empresaId;
        Proveedor = NormalizarRequerido(proveedor, LongitudMaximaProveedor, nameof(proveedor));
        EventoExternoId = NormalizarRequerido(
            eventoExternoId,
            LongitudMaximaEventoExternoId,
            nameof(eventoExternoId));
        TipoEvento = NormalizarRequerido(tipoEvento, LongitudMaximaTipoEvento, nameof(tipoEvento));
        PayloadHash = NormalizarRequerido(payloadHash, LongitudMaximaPayloadHash, nameof(payloadHash));
        CorrelationId = NormalizarOpcional(correlationId, LongitudMaximaCorrelationId, nameof(correlationId));
        EmitidoEnUtc = ExigirUtc(emitidoEnUtc, nameof(emitidoEnUtc));
        RecibidoEnUtc = ExigirUtc(recibidoEnUtc, nameof(recibidoEnUtc));
        Estado = EstadoWebhookEntrante.Recibido;
    }

    public int EmpresaId { get; private set; }
    public string Proveedor { get; private set; } = string.Empty;
    public string EventoExternoId { get; private set; } = string.Empty;
    public string TipoEvento { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public DateTime EmitidoEnUtc { get; private set; }
    public DateTime RecibidoEnUtc { get; private set; }
    public EstadoWebhookEntrante Estado { get; private set; } = EstadoWebhookEntrante.Recibido;
    public DateTime? ProcesandoDesdeUtc { get; private set; }
    public DateTime? ProcesadoEnUtc { get; private set; }
    public DateTime? RechazadoEnUtc { get; private set; }
    public string? MotivoRechazo { get; private set; }

    public bool EsTerminal => Estado is EstadoWebhookEntrante.Procesado or EstadoWebhookEntrante.Rechazado;

    public static WebhookEntrante CrearVerificado(
        int empresaId,
        string proveedor,
        string eventoExternoId,
        string tipoEvento,
        string payloadHash,
        string? correlationId,
        DateTime emitidoEnUtc,
        DateTime recibidoEnUtc)
    {
        return new WebhookEntrante(
            empresaId,
            proveedor,
            eventoExternoId,
            tipoEvento,
            payloadHash,
            correlationId,
            emitidoEnUtc,
            recibidoEnUtc);
    }

    /// <summary>
    /// Permite decidir replay idempotente sin persistir el payload original.
    /// Un mismo proveedor/evento externo con una huella distinta debe tratarse
    /// como conflicto por la capa de aplicación/persistencia.
    /// </summary>
    public bool CoincidePayload(string payloadHash)
    {
        var normalizado = NormalizarRequerido(payloadHash, LongitudMaximaPayloadHash, nameof(payloadHash));
        return string.Equals(PayloadHash, normalizado, StringComparison.OrdinalIgnoreCase);
    }

    public void MarcarProcesando(DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado != EstadoWebhookEntrante.Recibido)
            throw new InvalidOperationException($"El webhook en estado {Estado} no puede pasar a Procesando.");

        Estado = EstadoWebhookEntrante.Procesando;
        ProcesandoDesdeUtc = ahoraUtc;
        FechaActualizacion = ahoraUtc;
    }

    public void MarcarProcesado(DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado != EstadoWebhookEntrante.Procesando)
            throw new InvalidOperationException("Solo un webhook en procesamiento puede marcarse como procesado.");

        Estado = EstadoWebhookEntrante.Procesado;
        ProcesadoEnUtc = ahoraUtc;
        ProcesandoDesdeUtc = null;
        MotivoRechazo = null;
        FechaActualizacion = ahoraUtc;
    }

    public void MarcarRechazado(string motivo, DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (EsTerminal)
            throw new InvalidOperationException($"El webhook en estado {Estado} ya es terminal.");

        MotivoRechazo = NormalizarRequerido(motivo, LongitudMaximaMotivoRechazo, nameof(motivo));
        Estado = EstadoWebhookEntrante.Rechazado;
        RechazadoEnUtc = ahoraUtc;
        ProcesandoDesdeUtc = null;
        FechaActualizacion = ahoraUtc;
    }

    private static string NormalizarRequerido(string? value, int longitudMaxima, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El valor es obligatorio.", parametro);

        var normalizado = value.Trim();
        if (normalizado.Length > longitudMaxima)
            throw new ArgumentException($"El valor excede {longitudMaxima} caracteres.", parametro);

        return normalizado;
    }

    private static string? NormalizarOpcional(string? value, int longitudMaxima, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalizado = value.Trim();
        if (normalizado.Length > longitudMaxima)
            throw new ArgumentException($"El valor excede {longitudMaxima} caracteres.", parametro);

        return normalizado;
    }

    private static DateTime ExigirUtc(DateTime value, string parametro)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", parametro);

        return value;
    }
}
