using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public class N72DOutboxRetryPolicyTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void PrimerFallo_ProgramaDemoraBase_SinJitter()
    {
        var policy = new OutboxRetryPolicy(jitterRatio: 0);

        var decision = policy.EvaluarDespuesDeFallo(1, AhoraUtc, 0.5);

        Assert.True(decision.DebeReintentar);
        Assert.False(decision.DeadLetter);
        Assert.Equal(TimeSpan.FromSeconds(5), decision.Demora);
        Assert.Equal(AhoraUtc.AddSeconds(5), decision.DisponibleDesdeUtc);
    }

    [Fact]
    public void Backoff_EsExponencial_Y_QuedaLimitadoPorCap()
    {
        var policy = new OutboxRetryPolicy(
            maximoIntentos: 20,
            demoraBase: TimeSpan.FromSeconds(5),
            demoraMaxima: TimeSpan.FromSeconds(30),
            jitterRatio: 0);

        var segundo = policy.EvaluarDespuesDeFallo(2, AhoraUtc, 0.5);
        var cuarto = policy.EvaluarDespuesDeFallo(4, AhoraUtc, 0.5);

        Assert.Equal(TimeSpan.FromSeconds(10), segundo.Demora);
        Assert.Equal(TimeSpan.FromSeconds(30), cuarto.Demora);
    }

    [Fact]
    public void Jitter_RespetaVentanaConfigurada_Y_EsDeterministaPorMuestra()
    {
        var policy = new OutboxRetryPolicy(jitterRatio: 0.20);

        var minimo = policy.EvaluarDespuesDeFallo(1, AhoraUtc, 0);
        var centro = policy.EvaluarDespuesDeFallo(1, AhoraUtc, 0.5);
        var maximo = policy.EvaluarDespuesDeFallo(1, AhoraUtc, 1);
        var repetido = policy.EvaluarDespuesDeFallo(1, AhoraUtc, 1);

        Assert.Equal(TimeSpan.FromSeconds(4), minimo.Demora);
        Assert.Equal(TimeSpan.FromSeconds(5), centro.Demora);
        Assert.Equal(TimeSpan.FromSeconds(6), maximo.Demora);
        Assert.Equal(maximo, repetido);
    }

    [Fact]
    public void MaximoIntentos_EnviaADeadLetter_SinNuevaVentana()
    {
        var policy = new OutboxRetryPolicy(maximoIntentos: 3);

        var decision = policy.EvaluarDespuesDeFallo(3, AhoraUtc, 0.5);

        Assert.False(decision.DebeReintentar);
        Assert.True(decision.DeadLetter);
        Assert.Null(decision.Demora);
        Assert.Null(decision.DisponibleDesdeUtc);
    }

    [Fact]
    public void ConfiguracionSinLimitesYOverflow_FallanCerrado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OutboxRetryPolicy(maximoIntentos: OutboxRetryPolicy.MaximoIntentosPermitido + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OutboxRetryPolicy(demoraMaxima: OutboxRetryPolicy.DemoraMaximaPermitida.Add(TimeSpan.FromTicks(1))));

        var policy = new OutboxRetryPolicy(jitterRatio: 0);
        var casiMaximo = DateTime.SpecifyKind(
            DateTime.MaxValue.Subtract(TimeSpan.FromSeconds(1)),
            DateTimeKind.Utc);

        Assert.Throws<InvalidOperationException>(() =>
            policy.EvaluarDespuesDeFallo(1, casiMaximo, 0.5));
    }

    [Fact]
    public void RechazaFechaNoUtc_Y_MuestraJitterFueraDeRango()
    {
        var policy = new OutboxRetryPolicy();
        var local = DateTime.SpecifyKind(AhoraUtc, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => policy.EvaluarDespuesDeFallo(1, local, 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.EvaluarDespuesDeFallo(1, AhoraUtc, -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.EvaluarDespuesDeFallo(1, AhoraUtc, 1.01));
    }
}
