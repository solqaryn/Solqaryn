using Solqaryn.Domain.Governance;
using Xunit;

namespace Solqaryn.Tests.Domain;

/// <summary>
/// C00-04.M05.01: deterministic regression of the existing pure domain rule.
/// These fixtures are not PLAN/COLA/DAG or verified identity/authorization.
/// </summary>
public sealed class C0004M0501GovernanceRulesRegressionTests
{
    private const string Sha = "50ad7ff7082ac7868293c59f144c5d3b5365844b";

    private static PropuestaCambioGobernado Create(
        string microtask = "C00-04.M05.01",
        string decision = "D01",
        string motive = "Cambio reversible documentado en dev",
        string key = "gov:test:00001",
        TipoCambioGobierno type = TipoCambioGobierno.Documental,
        long revision = 1,
        bool rollback = true,
        string evidenceSha = Sha) =>
        GobiernoCambiosReglas.CrearPropuesta(
            microtask, decision, motive, evidenceSha, key, revision,
            type, rollback);

    [Theory]
    [InlineData("C00-04.M05.01", "D01")]
    [InlineData("C99-99.M99.99", "D99")]
    public void FormatoNuncaDemuestraCatalogoNiAutorizacion(string task, string decision)
    {
        var result = Create(microtask: task, decision: decision);
        Assert.Equal(task, result.MicrotareaId);
        Assert.Equal(decision, result.DecisionId);
        Assert.Equal(EvaluacionCambioGobierno.PendienteAutorizacion, result.Evaluacion);
        Assert.False(result.AutorizaEjecucion);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(128)]
    public void ClaveEnExtremosValidosSePreservaSinConcederPermisos(int length)
    {
        var key = new string('A', length);
        var result = Create(key: key);
        Assert.Equal(key, result.IdempotencyKey);
        Assert.False(result.AutorizaEjecucion);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(129)]
    public void ClaveFueraDelLimiteSeRechaza(int length)
    {
        Assert.Throws<ArgumentException>(() => Create(key: new string('A', length)));
    }

    [Fact]
    public void MotivoExactamenteSeiscientosCaracteresEsLimiteValido()
    {
        var motive = new string('M', 600);
        var result = Create(motive: motive);
        Assert.Equal(motive, result.Motivo);
        Assert.False(result.AutorizaEjecucion);
        Assert.Throws<ArgumentException>(() => Create(motive: motive + "X"));
    }

    [Theory]
    [InlineData("Linea\ninyectada")]
    [InlineData("Linea\rinyectada")]
    [InlineData("Control\tsegmentado")]
    public void ControlesEnMotivoSeRechazanAunqueNoHayaEscritura(string motive)
    {
        Assert.Throws<ArgumentException>(() => Create(motive: motive));
    }

    [Theory]
    [InlineData(TipoCambioGobierno.Documental)]
    [InlineData(TipoCambioGobierno.CodigoDev)]
    public void SinRollbackNiCambioOrdinarioSePuedeAutorizar(TipoCambioGobierno type)
    {
        var result = Create(type: type, rollback: false);
        Assert.Equal(EvaluacionCambioGobierno.NoGo, result.Evaluacion);
        Assert.False(result.AutorizaEjecucion);
    }

    [Theory]
    [InlineData(TipoCambioGobierno.SeguridadYPermisos)]
    [InlineData(TipoCambioGobierno.PersistenciaYMigracion)]
    [InlineData(TipoCambioGobierno.InfraestructuraYProduccion)]
    public void CambiosSensiblesNoGoAunConRollback(TipoCambioGobierno type)
    {
        var result = Create(type: type, rollback: true);
        Assert.Equal(EvaluacionCambioGobierno.NoGo, result.Evaluacion);
        Assert.False(result.AutorizaEjecucion);
    }

    [Fact]
    public void RevisionMaximaYEvidenciaHexadecimalNoElevanPrivilegios()
    {
        var evidence = Sha.ToUpperInvariant();
        var result = Create(revision: long.MaxValue, evidenceSha: evidence);
        Assert.Equal(long.MaxValue, result.RevisionEsperada);
        Assert.Equal(evidence, result.EvidenciaSha);
        Assert.False(result.AutorizaEjecucion);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void RevisionNoPositivaSeRechazaAntesDeEmitirPropuesta(long revision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(revision: revision));
    }
}
