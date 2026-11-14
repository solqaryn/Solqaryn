using Solqaryn.Application.Governance;
using Solqaryn.Domain.Governance;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class GobiernoCambiosPreparacionServiceTests
{
    private const string Sha = "a1c72e0d59446abd281d88ab83da8adddb9abdf2";
    private readonly GobiernoCambiosPreparacionService _service = new();

    private static SolicitudPreparacionCambio Solicitud(
        TipoCambioGobierno tipo = TipoCambioGobierno.Documental,
        bool rollback = true) =>
        new("C00-04.M03.02", "D01", "Consulta interna DEV con rollback",
            Sha, "governance:proposal:0001", 7, tipo, rollback);

    [Theory]
    [InlineData(TipoCambioGobierno.Documental)]
    [InlineData(TipoCambioGobierno.CodigoDev)]
    public void DocumentalOCodigoSoloGeneranPreparacionPendiente(TipoCambioGobierno tipo)
    {
        var request = Solicitud(tipo);
        var result = _service.Preparar(request);

        Assert.Equal("PENDIENTE_AUTORIZACION", result.Estado);
        Assert.Equal(EvaluacionCambioGobierno.PendienteAutorizacion, result.Evaluacion);
        Assert.Equal(request.MicrotareaId, result.MicrotareaId);
        Assert.Equal(request.DecisionId, result.DecisionId);
        Assert.Equal(request.EvidenciaSha, result.EvidenciaSha);
        Assert.Equal(7, result.RevisionEsperada);
        Assert.False(result.AutorizaEjecucion);
        Assert.False(result.AutorizaPersistencia);
        Assert.False(result.AutorizaConcesionPermisos);
    }

    [Theory]
    [InlineData(TipoCambioGobierno.SeguridadYPermisos)]
    [InlineData(TipoCambioGobierno.PersistenciaYMigracion)]
    [InlineData(TipoCambioGobierno.InfraestructuraYProduccion)]
    public void CambioSensibleSiempreNoGoInclusoConRollback(TipoCambioGobierno tipo)
    {
        var result = _service.Preparar(Solicitud(tipo));

        Assert.Equal("NO_GO", result.Estado);
        Assert.Equal(EvaluacionCambioGobierno.NoGo, result.Evaluacion);
        Assert.False(result.AutorizaEjecucion);
        Assert.False(result.AutorizaPersistencia);
        Assert.False(result.AutorizaConcesionPermisos);
    }

    [Fact]
    public void SinRollbackNoGoYNoProduceEfectos()
    {
        var request = Solicitud(rollback: false);
        var first = _service.Preparar(request);
        var second = _service.Preparar(request);

        Assert.Equal(first, second);
        Assert.Equal("NO_GO", first.Estado);
        Assert.False(first.AutorizaEjecucion);
    }

    [Fact]
    public void MotivoYClaveIdempotenciaNoSalenEnProyeccion()
    {
        var result = _service.Preparar(Solicitud() with
        {
            Motivo = "Contenido interno sensible que no se devuelve",
            IdempotencyKey = "governance:secret:unique"
        });

        Assert.DoesNotContain("Motivo", result.GetType().GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("IdempotencyKey", result.GetType().GetProperties().Select(x => x.Name));
        Assert.Equal("PENDIENTE_AUTORIZACION", result.Estado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("C00-04.M03.02\n")]
    [InlineData("C00-04.M03")]
    public void IdentificadorMalformadoSeRechaza(string? id)
    {
        Assert.Throws<ArgumentException>(() =>
            _service.Preparar(Solicitud() with { MicrotareaId = id }));
    }

    [Fact]
    public void EvidenciaManipuladaNoSeAcepta()
    {
        Assert.Throws<ArgumentException>(() =>
            _service.Preparar(Solicitud() with { EvidenciaSha = "abcdef" }));
    }

    [Fact]
    public void RevisionNulaODesconocidaNoSeAcepta()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.Preparar(Solicitud() with { RevisionEsperada = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.Preparar(Solicitud((TipoCambioGobierno)99)));
    }

    [Fact]
    public void SinSolicitudSeRechaza()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Preparar(null!));
    }
}
