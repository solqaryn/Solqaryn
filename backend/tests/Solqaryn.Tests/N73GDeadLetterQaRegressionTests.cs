using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N73GDeadLetterQaRegressionTests
{
    [Fact]
    public void QaRegression_Reintentos_Agotados_Terminan_En_DeadLetter_Y_No_Reabren()
    {
        var now = new DateTime(2026, 9, 13, 18, 0, 0, DateTimeKind.Utc);
        var mensaje = MensajeOutbox.Crear(
            73,
            "n7.3.g-qa",
            "{}",
            "n73g-dead-letter",
            correlationId: "n73g-correlation",
            creadoEnUtc: now);

        mensaje.MarcarProcesando(now);
        mensaje.RegistrarFallo(
            "primer fallo",
            now.AddSeconds(1),
            now.AddMinutes(1),
            maximoIntentos: 2);

        Assert.Equal(EstadoMensajeOutbox.Fallido, mensaje.Estado);
        Assert.Equal(1, mensaje.Intentos);
        Assert.False(mensaje.EsTerminal);
        Assert.Throws<InvalidOperationException>(() => mensaje.MarcarProcesando(now.AddSeconds(30)));

        mensaje.MarcarProcesando(now.AddMinutes(1));
        mensaje.RegistrarFallo(
            "segundo fallo",
            now.AddMinutes(1).AddSeconds(1),
            now.AddMinutes(2),
            maximoIntentos: 2);

        Assert.Equal(EstadoMensajeOutbox.DeadLetter, mensaje.Estado);
        Assert.Equal(2, mensaje.Intentos);
        Assert.True(mensaje.EsTerminal);
        Assert.Null(mensaje.ProcesandoDesdeUtc);
        Assert.Equal("segundo fallo", mensaje.UltimoError);
        Assert.Throws<InvalidOperationException>(() => mensaje.MarcarProcesando(now.AddMinutes(3)));
    }

    [Fact]
    public void QaRegression_Tenant_E_Idempotencia_Permanecen_Aislados_Durante_Reintentos()
    {
        const string sharedKey = "n73g-shared-key";
        var now = new DateTime(2026, 9, 13, 18, 0, 0, DateTimeKind.Utc);
        var tenant73 = MensajeOutbox.Crear(73, "n7.3.g-qa", "{}", sharedKey, creadoEnUtc: now);
        var tenant74 = MensajeOutbox.Crear(74, "n7.3.g-qa", "{}", sharedKey, creadoEnUtc: now);

        tenant73.MarcarProcesando(now);
        tenant73.RegistrarFallo(
            "tenant 73 failure",
            now.AddSeconds(1),
            now.AddMinutes(1),
            maximoIntentos: 3);

        Assert.Equal(73, tenant73.EmpresaId);
        Assert.Equal(74, tenant74.EmpresaId);
        Assert.Equal(sharedKey, tenant73.ClaveIdempotencia);
        Assert.Equal(sharedKey, tenant74.ClaveIdempotencia);
        Assert.Equal(1, tenant73.Intentos);
        Assert.Equal(0, tenant74.Intentos);
        Assert.Equal(EstadoMensajeOutbox.Fallido, tenant73.Estado);
        Assert.Equal(EstadoMensajeOutbox.Pendiente, tenant74.Estado);
    }

    [Fact]
    public void QaRegression_Entrega_Terminal_No_Admite_Nuevo_Procesamiento()
    {
        var now = new DateTime(2026, 9, 13, 18, 0, 0, DateTimeKind.Utc);
        var mensaje = MensajeOutbox.Crear(
            73,
            "n7.3.g-qa",
            "{}",
            "n73g-delivered",
            creadoEnUtc: now);

        mensaje.MarcarProcesando(now);
        mensaje.MarcarEntregado(now.AddSeconds(1));

        Assert.Equal(EstadoMensajeOutbox.Entregado, mensaje.Estado);
        Assert.True(mensaje.EsTerminal);
        Assert.Equal(1, mensaje.Intentos);
        Assert.Throws<InvalidOperationException>(() => mensaje.MarcarProcesando(now.AddMinutes(1)));
    }
}
