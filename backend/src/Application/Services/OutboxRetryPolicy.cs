namespace Solqaryn.Application.Services;

public sealed record OutboxRetryDecision(
    bool DebeReintentar,
    bool DeadLetter,
    DateTime? DisponibleDesdeUtc,
    TimeSpan? Demora);

/// <summary>
/// Política determinista de reintentos para el outbox. El llamador suministra
/// una muestra de jitter en [0,1] para permitir pruebas reproducibles y evitar
/// sincronizar múltiples workers sobre el mismo instante de reintento.
/// </summary>
public sealed class OutboxRetryPolicy
{
    public const int MaximoIntentosPredeterminado = 5;
    public const int MaximoIntentosPermitido = 100;
    public static readonly TimeSpan DemoraMaximaPermitida = TimeSpan.FromDays(7);
    public static readonly TimeSpan DemoraBasePredeterminada = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DemoraMaximaPredeterminada = TimeSpan.FromMinutes(5);

    private readonly int _maximoIntentos;
    private readonly TimeSpan _demoraBase;
    private readonly TimeSpan _demoraMaxima;
    private readonly double _jitterRatio;

    public OutboxRetryPolicy(
        int maximoIntentos = MaximoIntentosPredeterminado,
        TimeSpan? demoraBase = null,
        TimeSpan? demoraMaxima = null,
        double jitterRatio = 0.20)
    {
        _demoraBase = demoraBase ?? DemoraBasePredeterminada;
        _demoraMaxima = demoraMaxima ?? DemoraMaximaPredeterminada;

        if (maximoIntentos is <= 0 or > MaximoIntentosPermitido)
            throw new ArgumentOutOfRangeException(nameof(maximoIntentos));
        if (_demoraBase <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(demoraBase));
        if (_demoraMaxima < _demoraBase || _demoraMaxima > DemoraMaximaPermitida)
            throw new ArgumentOutOfRangeException(nameof(demoraMaxima));
        if (jitterRatio is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(jitterRatio));

        _maximoIntentos = maximoIntentos;
        _jitterRatio = jitterRatio;
    }

    public OutboxRetryDecision EvaluarDespuesDeFallo(
        int intentosRealizados,
        DateTime ahoraUtc,
        double muestraJitter)
    {
        if (intentosRealizados <= 0)
            throw new ArgumentOutOfRangeException(nameof(intentosRealizados));
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(ahoraUtc));
        if (muestraJitter is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(muestraJitter));

        if (intentosRealizados >= _maximoIntentos)
            return new OutboxRetryDecision(false, true, null, null);

        var exponente = Math.Min(intentosRealizados - 1, 30);
        var multiplicador = Math.Pow(2, exponente);
        var ticksSinJitter = Math.Min(
            _demoraMaxima.Ticks,
            _demoraBase.Ticks * multiplicador);

        var jitterCentrado = (muestraJitter * 2d) - 1d;
        var ticksConJitter = ticksSinJitter * (1d + (jitterCentrado * _jitterRatio));
        var ticksFinales = (long)Math.Clamp(
            ticksConJitter,
            1d,
            _demoraMaxima.Ticks);
        var demora = TimeSpan.FromTicks(ticksFinales);
        if (ahoraUtc.Ticks > DateTime.MaxValue.Ticks - demora.Ticks)
            throw new InvalidOperationException("La próxima disponibilidad excede el rango de DateTime.");

        return new OutboxRetryDecision(
            true,
            false,
            ahoraUtc.Add(demora),
            demora);
    }
}
