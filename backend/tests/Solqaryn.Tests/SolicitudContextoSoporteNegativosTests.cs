using System;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class P1003ASolicitudContextoSoporteNegativosTests
{
    private const string Motivo =
        "Revision de incidente solicitada por soporte autorizado.";

    private sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 10, 30, 23, 0, 0, TimeSpan.Zero);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("demasiado corto")]
    [InlineData("Motivo de soporte válido\npero contiene salto de línea")]
    public void Rechaza_motivos_invalidos(string? motivo)
    {
        Assert.Throws<ArgumentException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(
                9, 101, motivo!, 10, new RelojFijo()));
    }

    [Theory]
    [InlineData("Motivo de soporte \u200B con marca invisible zero width")]
    [InlineData("Motivo de soporte \u202E con anulacion bidi oculta")]
    [InlineData("Motivo de soporte \u2066 con aislador bidi oculto")]
    [InlineData("Motivo de soporte \u2028 con separador de linea oculto")]
    [InlineData("Motivo de soporte \u2029 con separador de parrafo oculto")]
    public void Rechaza_formatos_unicode_invisibles_en_justificacion(string motivo)
    {
        Assert.Throws<ArgumentException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(
                9, 101, motivo, 10, new RelojFijo()));
    }

    [Fact]
    public void Rechaza_motivo_superior_al_maximo()
    {
        Assert.Throws<ArgumentException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(
                9, 101, new string('x', 501), 10, new RelojFijo()));
    }

    [Theory]
    [InlineData(-1, 101)]
    [InlineData(9, -1)]
    public void Rechaza_identidades_negativas(int actor, int empresa)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(
                actor, empresa, Motivo, 10, new RelojFijo()));
    }
}
