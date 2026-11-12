using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-005.F / PREPARAR CASO: matriz determinista de token de recuperacion.
/// Estas pruebas verifican SOLO el fixture y la huella sintetica, NO un servicio
/// real de reset, base de datos, limite HTTP, emision ni consumo de tokens.
/// </summary>
public sealed class I8005FTokenRecuperacionFixtureTests
{
    [Theory]
    // EmisorTenant, SolicitaTenant, SegundosRestantes, Usado, Revocado,
    // LimiteExcedido, DigestCoincide, PermitirSegunContrato
    [InlineData(11, 11, 90, false, false, false, true, true)]
    [InlineData(11, 22, 90, false, false, false, true, false)]
    [InlineData(11, 11, 0, false, false, false, true, false)]
    [InlineData(11, 11, -1, false, false, false, true, false)]
    [InlineData(11, 11, 90, true, false, false, true, false)]
    [InlineData(11, 11, 90, false, true, false, true, false)]
    [InlineData(11, 11, 90, false, false, true, true, false)]
    [InlineData(11, 11, 90, false, false, false, false, false)]
    public void MatrizDeFixture_IncluyeNegativosDeTenancyExpiracionUsoRevocacionYLimite(
        int tenantDeEmision,
        int tenantDeSolicitud,
        int segundosRestantes,
        bool usado,
        bool revocado,
        bool limiteExcedido,
        bool digestCoincide,
        bool permitirSegunContrato)
    {
        // Oraculo de especificacion del fixture, NO implementacion productiva.
        // Una futura prueba de integracion debe sustituir esta expresion por
        // una llamada al servicio real y comprobar consumo atomico en DB.
        var decisionEspecificada =
            tenantDeEmision == tenantDeSolicitud &&
            segundosRestantes > 0 &&
            !usado && !revocado && !limiteExcedido && digestCoincide;

        Assert.Equal(permitirSegunContrato, decisionEspecificada);
    }

    [Fact]
    public void HuellasSinteticas_DifierenDelTokenCrudoYEntreDosEmpresas()
    {
        const string tokenA = "fixture-i8005f-empresa11-token-sintetico";
        const string tokenB = "fixture-i8005f-empresa22-token-sintetico";
        var digestA = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenA)));
        var digestB = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenB)));

        Assert.Equal(64, digestA.Length);
        Assert.Equal(64, digestB.Length);
        Assert.NotEqual(digestA, digestB);
        Assert.DoesNotContain(tokenA, digestA, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(tokenB, digestB, StringComparison.OrdinalIgnoreCase);
    }
}
