using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class RetryMappingContractTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 12, 35, 0, DateTimeKind.Utc);

    [Fact]
    public void Primer_Fallo_Se_Mapea_A_Reintento_Con_Demora_Base()
    {
        var policy = new OutboxRetryPolicy(
            maximoIntentos: 5,
            demoraBase: TimeSpan.FromSeconds(5),
            demoraMaxima: TimeSpan.FromMinutes(5),
            jitterRatio: 0);

        var decision = policy.EvaluarDespuesDeFallo(1, AhoraUtc, 0.5);

        Assert.True(decision.DebeReintentar);
        Assert.False(decision.DeadLetter);
        Assert.Equal(TimeSpan.FromSeconds(5), decision.Demora);
        Assert.Equal(AhoraUtc.AddSeconds(5), decision.DisponibleDesdeUtc);
    }

    [Fact]
    public void Intento_Final_Se_Mapea_A_DeadLetter_Sin_Proxima_Ventana()
    {
        var policy = new OutboxRetryPolicy(maximoIntentos: 3, jitterRatio: 0);

        var decision = policy.EvaluarDespuesDeFallo(3, AhoraUtc, 0.5);

        Assert.False(decision.DebeReintentar);
        Assert.True(decision.DeadLetter);
        Assert.Null(decision.Demora);
        Assert.Null(decision.DisponibleDesdeUtc);
    }

    [Fact]
    public void Backoff_Exponencial_Respeta_El_Cap_Configurado()
    {
        var policy = new OutboxRetryPolicy(
            maximoIntentos: 100,
            demoraBase: TimeSpan.FromSeconds(10),
            demoraMaxima: TimeSpan.FromSeconds(30),
            jitterRatio: 0);

        var decision = policy.EvaluarDespuesDeFallo(20, AhoraUtc, 0.5);

        Assert.True(decision.DebeReintentar);
        Assert.Equal(TimeSpan.FromSeconds(30), decision.Demora);
        Assert.Equal(AhoraUtc.AddSeconds(30), decision.DisponibleDesdeUtc);
    }
}
