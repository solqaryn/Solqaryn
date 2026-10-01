using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Intención durable de ejecutar un efecto externo después de confirmar la
/// transacción de negocio que lo originó. La persistencia/claim/dispatcher se
/// implementan en microtareas posteriores; este tipo fija las invariantes de
/// dominio para impedir envíos sin identidad tenant e idempotencia durable.
/// </summary>
public sealed class MensajeOutbox : BaseEntity
{
    public const int LongitudMaximaTipoEvento = 160;
    public const int LongitudMaximaTipoAgregado = 120;
    public const int LongitudMaximaIdAgregado = 160;
    public const int LongitudMaximaClaveIdempotencia = 200;
    public const int LongitudMaximaCorrelationId = 160;
    public const int LongitudMaximaError = 2000;
    public const int LongitudMaximaPayload = 65535;

    private MensajeOutbox()
    {
    }

    private MensajeOutbox(
        int empresaId,
        string tipoEvento,
        string payloadJson,
        string claveIdempotencia,
        string? tipoAgregado,
        string? idAgregado,
        string? correlationId,
        DateTime creadoEnUtc)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa/tenant del mensaje outbox es obligatoria.");

        EmpresaId = empresaId;
        EventoId = Guid.NewGuid();
        TipoEvento = NormalizarRequerido(tipoEvento, LongitudMaximaTipoEvento, nameof(tipoEvento));
        PayloadJson = NormalizarRequerido(payloadJson, LongitudMaximaPayload, nameof(payloadJson));
        ClaveIdempotencia = NormalizarRequerido(
            claveIdempotencia,
            LongitudMaximaClaveIdempotencia,
            nameof(claveIdempotencia));
        TipoAgregado = NormalizarOpcional(tipoAgregado, LongitudMaximaTipoAgregado, nameof(tipoAgregado));
        IdAgregado = NormalizarOpcional(idAgregado, LongitudMaximaIdAgregado, nameof(idAgregado));
        CorrelationId = NormalizarOpcional(correlationId, LongitudMaximaCorrelationId, nameof(correlationId));
        CreadoEnUtc = ExigirUtc(creadoEnUtc, nameof(creadoEnUtc));
        DisponibleDesdeUtc = CreadoEnUtc;
        Estado = EstadoMensajeOutbox.Pendiente;
    }

    public Guid EventoId { get; private set; }
    public int EmpresaId { get; private set; }
    public string TipoEvento { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string ClaveIdempotencia { get; private set; } = string.Empty;
    public string? TipoAgregado { get; private set; }
    public string? IdAgregado { get; private set; }
    public string? CorrelationId { get; private set; }

    public EstadoMensajeOutbox Estado { get; private set; } = EstadoMensajeOutbox.Pendiente;
    public int Intentos { get; private set; }
    public DateTime CreadoEnUtc { get; private set; }
    public DateTime DisponibleDesdeUtc { get; private set; }
    public DateTime? ProcesandoDesdeUtc { get; private set; }
    public DateTime? EntregadoEnUtc { get; private set; }
    public DateTime? UltimoIntentoEnUtc { get; private set; }
    public string? UltimoError { get; private set; }

    public bool EsTerminal => Estado is EstadoMensajeOutbox.Entregado or EstadoMensajeOutbox.DeadLetter;

    public static MensajeOutbox Crear(
        int empresaId,
        string tipoEvento,
        string payloadJson,
        string claveIdempotencia,
        string? tipoAgregado = null,
        string? idAgregado = null,
        string? correlationId = null,
        DateTime? creadoEnUtc = null)
    {
        return new MensajeOutbox(
            empresaId,
            tipoEvento,
            payloadJson,
            claveIdempotencia,
            tipoAgregado,
            idAgregado,
            correlationId,
            creadoEnUtc ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Marca el comienzo de un intento. El claim atómico contra múltiples
    /// procesos pertenece al repositorio/persistencia; este método protege la
    /// transición de estado una vez que un worker obtuvo el claim.
    /// </summary>
    public void MarcarProcesando(DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado is not (EstadoMensajeOutbox.Pendiente or EstadoMensajeOutbox.Fallido))
            throw new InvalidOperationException($"El mensaje outbox en estado {Estado} no puede pasar a Procesando.");

        if (ahoraUtc < DisponibleDesdeUtc)
            throw new InvalidOperationException("El mensaje outbox todavía no está disponible para reintento.");

        Estado = EstadoMensajeOutbox.Procesando;
        Intentos++;
        ProcesandoDesdeUtc = ahoraUtc;
        UltimoIntentoEnUtc = ahoraUtc;
        UltimoError = null;
        FechaActualizacion = ahoraUtc;
    }

    public void MarcarEntregado(DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado != EstadoMensajeOutbox.Procesando)
            throw new InvalidOperationException("Solo un mensaje outbox en procesamiento puede marcarse como entregado.");

        Estado = EstadoMensajeOutbox.Entregado;
        EntregadoEnUtc = ahoraUtc;
        ProcesandoDesdeUtc = null;
        UltimoError = null;
        FechaActualizacion = ahoraUtc;
    }

    /// <summary>
    /// Registra un fallo del intento activo. Si se agotó el máximo de intentos,
    /// el mensaje queda en DeadLetter; de lo contrario queda Fallido y se fija
    /// explícitamente la próxima ventana de reintento.
    /// </summary>
    public void RegistrarFallo(
        string error,
        DateTime ahoraUtc,
        DateTime disponibleDesdeUtc,
        int maximoIntentos)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));
        disponibleDesdeUtc = ExigirUtc(disponibleDesdeUtc, nameof(disponibleDesdeUtc));

        if (Estado != EstadoMensajeOutbox.Procesando)
            throw new InvalidOperationException("Solo un mensaje outbox en procesamiento puede registrar un fallo.");

        if (maximoIntentos <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximoIntentos), "El máximo de intentos debe ser mayor que cero.");

        var agotado = Intentos >= maximoIntentos;
        if (!agotado && disponibleDesdeUtc < ahoraUtc)
            throw new ArgumentOutOfRangeException(
                nameof(disponibleDesdeUtc),
                "La próxima disponibilidad no puede quedar en el pasado.");

        UltimoError = NormalizarRequerido(error, LongitudMaximaError, nameof(error));
        ProcesandoDesdeUtc = null;
        FechaActualizacion = ahoraUtc;

        if (agotado)
        {
            Estado = EstadoMensajeOutbox.DeadLetter;
            DisponibleDesdeUtc = ahoraUtc;
            return;
        }

        Estado = EstadoMensajeOutbox.Fallido;
        DisponibleDesdeUtc = disponibleDesdeUtc;
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
