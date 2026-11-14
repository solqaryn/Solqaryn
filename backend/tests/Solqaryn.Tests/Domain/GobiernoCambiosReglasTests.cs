using Solqaryn.Domain.Governance;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class GobiernoCambiosReglasTests
{
    private const string Sha = "50ad7ff7082ac7868293c59f144c5d3b5365844b";

    private static PropuestaCambioGobernado Proponer(
        string? microtarea = "C00-04.M03.01",
        string? decision = "D01",
        string? motivo = "Evaluar cambio reversible en DEV",
        string? evidencia = Sha,
        string? key = "scope:decision:001",
        long revision = 1,
        TipoCambioGobierno tipo = TipoCambioGobierno.Documental,
        bool rollback = true) =>
        GobiernoCambiosReglas.CrearPropuesta(
            microtarea, decision, motivo, evidencia, key, revision, tipo, rollback);

    [Theory]
    [InlineData(TipoCambioGobierno.Documental)]
    [InlineData(TipoCambioGobierno.CodigoDev)]
    public void PropuestaBienFormada_PermanecePendienteSinConcederEjecucion(TipoCambioGobierno tipo)
    {
        var propuesta = Proponer(tipo: tipo);
        Assert.Equal(EvaluacionCambioGobierno.PendienteAutorizacion, propuesta.Evaluacion);
        Assert.False(propuesta.AutorizaEjecucion);
        Assert.Equal("C00-04.M03.01", propuesta.MicrotareaId);
    }

    [Theory]
    [InlineData(TipoCambioGobierno.SeguridadYPermisos)]
    [InlineData(TipoCambioGobierno.PersistenciaYMigracion)]
    [InlineData(TipoCambioGobierno.InfraestructuraYProduccion)]
    public void CambiosSensibles_NoGoInclusoConRollback(TipoCambioGobierno tipo)
    {
        var propuesta = Proponer(tipo: tipo);
        Assert.Equal(EvaluacionCambioGobierno.NoGo, propuesta.Evaluacion);
        Assert.False(propuesta.AutorizaEjecucion);
    }

    [Fact]
    public void SinRollback_NoGoInclusoSiEsDocumental()
    {
        Assert.Equal(EvaluacionCambioGobierno.NoGo, Proponer(rollback: false).Evaluacion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C00-04.M03.01;DROP")]
    [InlineData("C00-04.M03")]
    public void MicrotareaSinFormatoValido_Rechazada(string? microtarea)
    {
        Assert.Throws<ArgumentException>(() => Proponer(microtarea: microtarea));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ADMIN")]
    [InlineData("D1")]
    public void DecisionInvalida_Rechazada(string? decision)
    {
        Assert.Throws<ArgumentException>(() => Proponer(decision: decision));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("Intento\ninyectado")]
    public void MotivoInvalido_Rechazado(string? motivo)
    {
        Assert.Throws<ArgumentException>(() => Proponer(motivo: motivo));
    }

    [Fact]
    public void MotivoExcesivo_Rechazado()
    {
        Assert.Throws<ArgumentException>(() => Proponer(motivo: new string('x', 601)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("50ad7ff7")]
    [InlineData("ZZad7ff7082ac7868293c59f144c5d3b5365844b")]
    public void EvidenciaNoShaCompleto_Rechazada(string? evidencia)
    {
        Assert.Throws<ArgumentException>(() => Proponer(evidencia: evidencia));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("tenant/other;run")]
    public void ClaveMalformada_Rechazada(string? key)
    {
        Assert.Throws<ArgumentException>(() => Proponer(key: key));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RevisionAusente_Rechazada(long revision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Proponer(revision: revision));
    }

    [Fact]
    public void TipoDesconocido_NoSeConvierteEnAutorizacion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Proponer(tipo: (TipoCambioGobierno)99));
    }
}
