using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Domain;

public class MensajeOutboxTests
{
    private static readonly DateTime Creado = new(2026, 9, 13, 4, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_ExigeTenantYClaveIdempotencia()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MensajeOutbox.Crear(0, "FacturaCorreoSolicitado", "{}", "factura:10:correo:abc", creadoEnUtc: Creado));

        Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "FacturaCorreoSolicitado", "{}", " ", creadoEnUtc: Creado));
    }

    [Fact]
    public void Crear_NormalizaContratoYQuedaPendiente()
    {
        var mensaje = MensajeOutbox.Crear(
            7,
            " FacturaCorreoSolicitado ",
            " {\"facturaId\":10} ",
            " factura:10:correo:abc ",
            " Factura ",
            " 10 ",
            " req-123 ",
            Creado);

        Assert.Equal(7, mensaje.EmpresaId);
        Assert.NotEqual(Guid.Empty, mensaje.EventoId);
        Assert.Equal("FacturaCorreoSolicitado", mensaje.TipoEvento);
        Assert.Equal("{\"facturaId\":10}", mensaje.PayloadJson);
        Assert.Equal("factura:10:correo:abc", mensaje.ClaveIdempotencia);
        Assert.Equal("Factura", mensaje.TipoAgregado);
        Assert.Equal("10", mensaje.IdAgregado);
        Assert.Equal("req-123", mensaje.CorrelationId);
        Assert.Equal(EstadoMensajeOutbox.Pendiente, mensaje.Estado);
        Assert.Equal(Creado, mensaje.CreadoEnUtc);
        Assert.Equal(Creado, mensaje.DisponibleDesdeUtc);
        Assert.Equal(0, mensaje.Intentos);
        Assert.False(mensaje.EsTerminal);
    }

    [Fact]
    public void ProcesarYEntregar_RespetaTransicionesTerminales()
    {
        var mensaje = CrearValido();
        var inicio = Creado.AddMinutes(1);
        var entrega = inicio.AddSeconds(5);

        mensaje.MarcarProcesando(inicio);
        mensaje.MarcarEntregado(entrega);

        Assert.Equal(EstadoMensajeOutbox.Entregado, mensaje.Estado);
        Assert.Equal(1, mensaje.Intentos);
        Assert.Equal(inicio, mensaje.UltimoIntentoEnUtc);
        Assert.Equal(entrega, mensaje.EntregadoEnUtc);
        Assert.True(mensaje.EsTerminal);
        Assert.Throws<InvalidOperationException>(() => mensaje.MarcarProcesando(entrega.AddMinutes(1)));
    }

    [Fact]
    public void FalloTransitorio_ProgramaReintentoYNoPermiteAdelantarlo()
    {
        var mensaje = CrearValido();
        var intento = Creado.AddMinutes(1);
        var siguiente = intento.AddMinutes(5);

        mensaje.MarcarProcesando(intento);
        mensaje.RegistrarFallo(" timeout SMTP ", intento.AddSeconds(1), siguiente, maximoIntentos: 3);

        Assert.Equal(EstadoMensajeOutbox.Fallido, mensaje.Estado);
        Assert.Equal("timeout SMTP", mensaje.UltimoError);
        Assert.Equal(siguiente, mensaje.DisponibleDesdeUtc);
        Assert.False(mensaje.EsTerminal);
        Assert.Throws<InvalidOperationException>(() => mensaje.MarcarProcesando(siguiente.AddSeconds(-1)));

        mensaje.MarcarProcesando(siguiente);
        Assert.Equal(EstadoMensajeOutbox.Procesando, mensaje.Estado);
        Assert.Equal(2, mensaje.Intentos);
    }

    [Fact]
    public void FalloConReintentoEnPasado_NoMutaMensaje()
    {
        var mensaje = CrearValido();
        var intento = Creado.AddMinutes(1);
        mensaje.MarcarProcesando(intento);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            mensaje.RegistrarFallo(
                "timeout SMTP",
                intento.AddSeconds(5),
                intento.AddSeconds(4),
                maximoIntentos: 3));

        Assert.Equal(EstadoMensajeOutbox.Procesando, mensaje.Estado);
        Assert.Equal(1, mensaje.Intentos);
        Assert.Equal(intento, mensaje.ProcesandoDesdeUtc);
        Assert.Null(mensaje.UltimoError);
        Assert.Equal(Creado, mensaje.DisponibleDesdeUtc);
    }

    [Fact]
    public void AgotarIntentos_MueveADeadLetterYBloqueaNuevoClaim()
    {
        var mensaje = CrearValido();
        var intento = Creado.AddMinutes(1);

        mensaje.MarcarProcesando(intento);
        mensaje.RegistrarFallo("rechazo permanente", intento.AddSeconds(1), intento.AddMinutes(1), maximoIntentos: 1);

        Assert.Equal(EstadoMensajeOutbox.DeadLetter, mensaje.Estado);
        Assert.True(mensaje.EsTerminal);
        Assert.Equal(1, mensaje.Intentos);
        Assert.Throws<InvalidOperationException>(() => mensaje.MarcarProcesando(intento.AddMinutes(2)));
    }

    [Fact]
    public void FechasNoUtc_SonRechazadas()
    {
        var local = DateTime.SpecifyKind(Creado, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "FacturaCorreoSolicitado", "{}", "clave-1", creadoEnUtc: local));

        var mensaje = CrearValido();
        Assert.Throws<ArgumentException>(() => mensaje.MarcarProcesando(local));
    }


    [Theory]
    [InlineData("Factura\r\nFalsa")]
    [InlineData("Factura\tFalsa")]
    [InlineData("Factura\0Falsa")]
    public void Crear_RechazaControlesEnTipoEvento(string entrada)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, entrada, "{}", "clave", creadoEnUtc: Creado));
        Assert.Equal("tipoEvento", ex.ParamName);
    }

    [Theory]
    [InlineData("id\r\nfalsa")]
    [InlineData("id\tfalsa")]
    [InlineData("id\0falsa")]
    public void Crear_RechazaControlesEnClaveIdempotencia(string entrada)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "Factura", "{}", entrada, creadoEnUtc: Creado));
        Assert.Equal("claveIdempotencia", ex.ParamName);
    }

    [Fact]
    public void Crear_RechazaControlesEnIdentificadoresOpcionales()
    {
        var tipo = Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "Factura", "{}", "clave", tipoAgregado: "Factura\nFalsa", creadoEnUtc: Creado));
        Assert.Equal("tipoAgregado", tipo.ParamName);
        var agregado = Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "Factura", "{}", "clave", idAgregado: "10\0Falso", creadoEnUtc: Creado));
        Assert.Equal("idAgregado", agregado.ParamName);
        var correlacion = Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "Factura", "{}", "clave", correlationId: "abc\tFalso", creadoEnUtc: Creado));
        Assert.Equal("correlationId", correlacion.ParamName);
    }

    [Fact]
    public void Crear_ConservaIdentificadoresValidosYJsonMultilineaEntreEmpresas()
    {
        const string json = "{\n  \"facturaId\": 10\n}";
        var a = MensajeOutbox.Crear(7, " Factura.Enviada/v2 ", json,
            " mensaje:ñ/2026 ", " Factura ", " 10 ", " solicitud-✓ ", Creado);
        var b = MensajeOutbox.Crear(8, "Factura.Enviada/v2", json,
            "mensaje:ñ/2026", creadoEnUtc: Creado);
        Assert.Equal("Factura.Enviada/v2", a.TipoEvento);
        Assert.Equal("mensaje:ñ/2026", a.ClaveIdempotencia);
        Assert.Equal("Factura", a.TipoAgregado);
        Assert.Equal("10", a.IdAgregado);
        Assert.Equal("solicitud-✓", a.CorrelationId);
        Assert.Equal(json, a.PayloadJson);
        Assert.Equal(7, a.EmpresaId);
        Assert.Equal(8, b.EmpresaId);
        Assert.Equal(a.ClaveIdempotencia, b.ClaveIdempotencia);
    }

    [Fact]
    public void Crear_RespetaLimitesYRechazaControlInclusoAlFinal()
    {
        var limite = new string('x', MensajeOutbox.LongitudMaximaClaveIdempotencia);
        var valido = MensajeOutbox.Crear(7, "Factura", "{}", limite, creadoEnUtc: Creado);
        Assert.Equal(limite, valido.ClaveIdempotencia);
        var exceso = Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "Factura", "{}", limite + "x", creadoEnUtc: Creado));
        Assert.Equal("claveIdempotencia", exceso.ParamName);
        var control = Assert.Throws<ArgumentException>(() =>
            MensajeOutbox.Crear(7, "Factura", "{}", "clave-valida\n", creadoEnUtc: Creado));
        Assert.Equal("claveIdempotencia", control.ParamName);
    }

    private static MensajeOutbox CrearValido() =>
        MensajeOutbox.Crear(
            7,
            "FacturaCorreoSolicitado",
            "{\"facturaId\":10}",
            "factura:10:correo:abc",
            "Factura",
            "10",
            "req-123",
            Creado);
}
