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

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(11, false)]
    public void Ventana_solicitada_usa_limite_superior_exclusivo(int minuto, bool esperado)
    {
        var solicitud = SolicitudContextoSoporte.CrearDesdeRelojServidor(
            9, 101, Motivo, 10, new RelojFijo(Fecha));

        Assert.Equal(esperado,
            solicitud.EstaDentroDeVentanaSolicitada(
                new RelojFijo(Fecha.AddMinutes(minuto))));
    }

    [Fact]
    public void Ultimo_tick_anterior_a_expiracion_pertenece_a_ventana()
    {
        var solicitud = SolicitudContextoSoporte.CrearDesdeRelojServidor(
            9, 101, Motivo, 10, new RelojFijo(Fecha));

        Assert.True(solicitud.EstaDentroDeVentanaSolicitada(
            new RelojFijo(Fecha.AddMinutes(10).AddTicks(-1))));
    }

    [Fact]
    public void Ventana_temporal_rechaza_reloj_nulo()
    {
        var solicitud = SolicitudContextoSoporte.CrearDesdeRelojServidor(
            9, 101, Motivo, 10, new RelojFijo(Fecha));

        Assert.Throws<ArgumentNullException>(() =>
            solicitud.EstaDentroDeVentanaSolicitada(null!));
    }

    [Fact]
    public void Ventana_normaliza_offset_suministrado_por_reloj_del_servidor()
    {
        var solicitud = SolicitudContextoSoporte.CrearDesdeRelojServidor(
            9, 101, Motivo, 10, new RelojFijo(Fecha));
        var local = new DateTimeOffset(2026, 10, 30, 17, 5, 0,
            TimeSpan.FromHours(-6));

        Assert.True(solicitud.EstaDentroDeVentanaSolicitada(new RelojFijo(local)));
    }

    [Fact]
    public void Rechaza_reloj_nulo()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SolicitudContextoSoporte.CrearDesdeRelojServidor(9, 101, Motivo, 10, null!));
    }
}
