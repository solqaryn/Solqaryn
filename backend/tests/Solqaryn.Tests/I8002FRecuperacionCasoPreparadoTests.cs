using System;
using System.Linq;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-002.F: matriz preparatoria reproducible, NO es una prueba de un endpoint de
/// recuperacion. Todo dato es sintetico y el envio SMTP queda fuera de este caso.
/// El resultado esperado solo define un futuro oraculo funcional, no un PASS runtime.
/// </summary>
public sealed class I8002FRecuperacionCasoPreparadoTests
{
    private const string MensajeNeutralEsperado =
        "Si existe una cuenta elegible, se enviaran instrucciones.";

    private sealed record Caso(
        string Codigo,
        string TenantSolicitado,
        string? TenantReal,
        string IdentidadSintetica,
        bool ExisteCuenta,
        bool NotificacionPermitida);

    private static readonly Caso[] Matriz =
    {
        new("MIEMBRO_A", "empresa-a", "empresa-a", "ana@empresa-a.invalid", true, true),
        new("INEXISTENTE_A", "empresa-a", null, "nadie@empresa-a.invalid", false, false),
        new("CRUCE_B_A", "empresa-a", "empresa-b", "luis@empresa-b.invalid", true, false)
    };

    [Fact]
    public void FixturePreparado_SeparaEfectoPrivadoDeRespuestaPublicaNeutral()
    {
        Assert.Equal(3, Matriz.Length);
        Assert.Equal(Matriz.Length, Matriz.Select(c => c.Codigo).Distinct().Count());
        Assert.Equal(Matriz.Length, Matriz.Select(c => c.IdentidadSintetica).Distinct().Count());

        Assert.Single(Matriz.Where(c => c.NotificacionPermitida));
        Assert.All(Matriz, c =>
        {
            Assert.EndsWith(".invalid", c.IdentidadSintetica, StringComparison.Ordinal);
            Assert.Equal("empresa-a", c.TenantSolicitado);
            Assert.False(string.IsNullOrWhiteSpace(MensajeNeutralEsperado));
            Assert.DoesNotContain(c.IdentidadSintetica, MensajeNeutralEsperado, StringComparison.Ordinal);
            Assert.DoesNotContain(c.TenantSolicitado, MensajeNeutralEsperado, StringComparison.Ordinal);
            if (c.NotificacionPermitida)
            {
                Assert.True(c.ExisteCuenta);
                Assert.Equal(c.TenantSolicitado, c.TenantReal);
            }
        });

        Assert.False(Matriz.Single(c => c.Codigo == "INEXISTENTE_A").NotificacionPermitida);
        Assert.False(Matriz.Single(c => c.Codigo == "CRUCE_B_A").NotificacionPermitida);
    }
}
