using Solqaryn.Infrastructure.Governance;
using Xunit;

namespace Solqaryn.Tests;

public sealed class GobiernoCambiosProyeccionAisladaTests
{
    private static LecturaGobiernoAislada Ejemplo(long revision = 1, string estado = "VALIDANDO") =>
        new("C00-04.M03.04", revision, "cff0f39c8286cf1ded1aafdcb1a08108a9016cae",
            estado, new DateTimeOffset(2026, 11, 14, 11, 30, 0, TimeSpan.Zero));

    [Fact]
    public void CopiaDeLecturaNoOtorgaAutoridad()
    {
        var s = new GobiernoCambiosProyeccionAislada();
        var v = Ejemplo();
        Assert.True(s.IntentarRegistrar(v));
        Assert.Equal(v, s.Obtener(v.MicrotareaId, 1));
        Assert.Equal("SOLQARYN:DEV", v.AmbitoInstitucional);
        Assert.False(v.AutorizaEjecucion);
        Assert.False(v.AutorizaAprobacion);
        Assert.False(v.AutorizaPersistenciaOficial);
        Assert.False(v.AutorizaConcesionPermisos);
        Assert.Equal(1, s.Cantidad);
    }

    [Fact]
    public void DuplicadoConflictivoNuncaReemplazaFuente()
    {
        var s = new GobiernoCambiosProyeccionAislada();
        var original = Ejemplo();
        Assert.True(s.IntentarRegistrar(original));
        Assert.False(s.IntentarRegistrar(original with { ShaFuente = new string('a', 40), EstadoObservado = "LISTO" }));
        Assert.Equal(original, s.Obtener(original.MicrotareaId, 1));
    }

    [Fact]
    public void VersionesSonIndependientesYSinFallback()
    {
        var s = new GobiernoCambiosProyeccionAislada();
        Assert.True(s.IntentarRegistrar(Ejemplo()));
        Assert.True(s.IntentarRegistrar(Ejemplo(2, "LISTO")));
        Assert.Equal("VALIDANDO", s.Obtener("C00-04.M03.04", 1)?.EstadoObservado);
        Assert.Equal("LISTO", s.Obtener("C00-04.M03.04", 2)?.EstadoObservado);
        Assert.Null(s.Obtener("C00-04.M03.04", 3));
    }

    [Theory]
    [InlineData("C00-04.M03.04\n")]
    [InlineData("C00-04.M03.04-EXTRA")]
    [InlineData("C00-04.M03.04|B")]
    public void IdentificadorManipuladoEsDenegado(string id)
    {
        var s = new GobiernoCambiosProyeccionAislada();
        Assert.Throws<ArgumentException>(() => s.IntentarRegistrar(Ejemplo() with { MicrotareaId = id }));
        Assert.Equal(0, s.Cantidad);
    }

    [Theory]
    [InlineData("APROBADO")]
    [InlineData("PROD")]
    [InlineData("VALIDANDO\n")]
    public void EstadoForjadoEsDenegado(string estado)
    {
        var s = new GobiernoCambiosProyeccionAislada();
        Assert.Throws<ArgumentException>(() => s.IntentarRegistrar(Ejemplo(estado: estado)));
    }

    [Fact]
    public void ShaInvalidoYRevisionNoPositivaSeDeniegan()
    {
        var s = new GobiernoCambiosProyeccionAislada();
        Assert.Throws<ArgumentException>(() => s.IntentarRegistrar(Ejemplo() with { ShaFuente = "bad" }));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.IntentarRegistrar(Ejemplo(0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Obtener("C00-04.M03.04", 0));
        Assert.Equal(0, s.Cantidad);
    }

    [Fact]
    public void FechaNoUtcSeDeniega()
    {
        var s = new GobiernoCambiosProyeccionAislada();
        Assert.Throws<ArgumentException>(() => s.IntentarRegistrar(Ejemplo() with
        {
            FechaLecturaUtc = new DateTimeOffset(2026, 11, 14, 5, 30, 0, TimeSpan.FromHours(-6))
        }));
        Assert.Equal(0, s.Cantidad);
    }

    [Fact]
    public async Task SesentaYCuatroConcurrentesTienenUnGanadorLocal()
    {
        var s = new GobiernoCambiosProyeccionAislada();
        var tasks = Enumerable.Range(0, 64)
            .Select(_ => Task.Run(() => s.IntentarRegistrar(Ejemplo()))).ToArray();
        var results = await Task.WhenAll(tasks);
        Assert.Single(results.Where(x => x));
        Assert.Equal(1, s.Cantidad);
    }

    [Fact]
    public void LecturaNoTieneTenantNiLeaseNiConcesion()
    {
        Assert.DoesNotContain(typeof(LecturaGobiernoAislada).GetProperties(),
            x => x.Name is "EmpresaId" or "TenantId" or "LeaseToken" or "PermisoOtorgado");
        Assert.False(Ejemplo(2, "LISTO").AutorizaEjecucion);
    }
}
