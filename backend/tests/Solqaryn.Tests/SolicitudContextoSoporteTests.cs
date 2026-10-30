using System;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class SolicitudContextoSoporteTests
{
    private static readonly DateTimeOffset AhoraUtc =
        new(2026, 10, 30, 23, 0, 0, TimeSpan.Zero);

    private const string Motivo =
        "Diagnóstico de incidente de facturación solicitado por soporte.";

    [Fact]
    public void Solicitud_valida_captura_tenant_motivo_y_expiracion_sin_otorgar_permisos()
    {
        var solicitud = new SolicitudContextoSoporte(
            9, 101, "  " + Motivo + "  ", AhoraUtc, AhoraUtc.AddMinutes(10));

        Assert.Equal(9, solicitud.UsuarioPlataformaId);
        Assert.Equal(101, solicitud.EmpresaDestinoId);
        Assert.Equal(Motivo, solicitud.Justificacion);
        Assert.Equal(AhoraUtc, solicitud.InicioUtc);
        Assert.Equal(AhoraUtc.AddMinutes(10), solicitud.ExpiracionUtc);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Rechaza_identidades_o_empresa_invalidas(int usuarioId, int empresaId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SolicitudContextoSoporte(usuarioId, empresaId, Motivo,
                AhoraUtc, AhoraUtc.AddMinutes(10)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("motivo insuficiente")]
    [InlineData("Justificación válida\ncon nueva línea")]
    public void Rechaza_justificaciones_invalidas(string? motivo)
    {
        Assert.Throws<ArgumentException>(() =>
            new SolicitudContextoSoporte(9, 101, motivo!,
                AhoraUtc, AhoraUtc.AddMinutes(10)));
    }

    [Fact]
    public void Rechaza_justificacion_demasiado_larga()
    {
        Assert.Throws<ArgumentException>(() =>
            new SolicitudContextoSoporte(9, 101, new string('a', 501),
                AhoraUtc, AhoraUtc.AddMinutes(10)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(16)]
    [InlineData(60)]
    public void Rechaza_duracion_nula_negativa_o_superior_a_quince_minutos(int minutos)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SolicitudContextoSoporte(9, 101, Motivo,
                AhoraUtc, AhoraUtc.AddMinutes(minutos)));
    }

    [Fact]
    public void Rechaza_fechas_con_offset_no_utc()
    {
        var desplazada = new DateTimeOffset(2026, 10, 30, 17, 0, 0, TimeSpan.FromHours(-6));
        Assert.Throws<ArgumentException>(() =>
            new SolicitudContextoSoporte(9, 101, Motivo,
                desplazada, AhoraUtc.AddMinutes(10)));
        Assert.Throws<ArgumentException>(() =>
            new SolicitudContextoSoporte(9, 101, Motivo,
                AhoraUtc, desplazada.AddMinutes(10)));
    }
}
