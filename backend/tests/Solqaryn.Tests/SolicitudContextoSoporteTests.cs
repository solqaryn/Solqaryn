using System;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class SolicitudContextoSoporteTests
{
    private const string Motivo = "Asistencia solicitada para investigar un incidente autorizado.";

    private sealed class RelojFijo : TimeProvider
    {
        private readonly DateTimeOffset _hora;
        public RelojFijo(DateTimeOffset hora) { _hora = hora; }
        public override DateTimeOffset GetUtcNow() => _hora;
    }

    private static readonly DateTimeOffset Fecha =
        new DateTimeOffset(2026, 10, 30, 23, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Genera_solicitud_con_reloj_del_servidor_y_expiracion_corta()
    {
        var solicitud = SolicitudContextoSoporte.CrearDesdeRelojServidor(
            9, 101, Motivo, 10, new RelojFijo(Fecha));
        Assert.Equal(9, solicitud.UsuarioPlataformaId);
        Assert.Equal(101, solicitud.EmpresaDestinoId);
        Assert.Equal(Motivo, solicitud.Justificacion);
        Assert.Equal(Fecha, solicitud.InicioUtc);
        Assert.Equal(Fecha.AddMinutes(10), solicitud.ExpiracionUtc);
    }

    [Fact]
    public void Convierte_offset_del_reloj_a_utc()
    {
        var local = new DateTimeOffset(2026, 10, 30, 17, 0, 0, TimeSpan.FromHours(-6));
        var solicitud = SolicitudContextoSoporte.CrearDesdeRelojServidor(
            9, 101, Motivo, 15, new RelojFijo(local));
        Assert.Equal(TimeSpan.Zero, solicitud.InicioUtc.Offset);
        Assert.Equal(Fecha, solicitud.InicioUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(16)]
    public void Rechaza_duraciones_fuera_de_rango(int minutos)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(
                9, 101, Motivo, minutos, new RelojFijo(Fecha)));
    }

    [Theory]
    [InlineData(0, 101)]
    [InlineData(9, 0)]
    public void Rechaza_identificadores_invalidos(int usuarioId, int empresaId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(
                usuarioId, empresaId, Motivo, 10, new RelojFijo(Fecha)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("corto")]
    public void Rechaza_motivos_invalidos(string motivo)
    {
        Assert.Throws<ArgumentException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(
                9, 101, motivo, 10, new RelojFijo(Fecha)));
    }

    [Fact]
    public void Rechaza_reloj_nulo()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(9, 101, Motivo, 10, null!));
    }
}
