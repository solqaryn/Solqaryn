using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Domain;

public class WebhookEntranteTests
{
    private static readonly DateTime Emitido = new(2026, 9, 13, 23, 40, 0, DateTimeKind.Utc);
    private static readonly DateTime Recibido = Emitido.AddSeconds(10);

    [Fact]
    public void CrearVerificado_ExigeTenantIdentidadYFechasUtc()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebhookEntrante.CrearVerificado(
                0,
                "stripe",
                "evt-1",
                "invoice.paid",
                "abc123",
                "req-1",
                Emitido,
                Recibido));

        Assert.Throws<ArgumentException>(() =>
            WebhookEntrante.CrearVerificado(
                7,
                " ",
                "evt-1",
                "invoice.paid",
                "abc123",
                "req-1",
                Emitido,
                Recibido));

        var local = DateTime.SpecifyKind(Emitido, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() =>
            WebhookEntrante.CrearVerificado(
                7,
                "stripe",
                "evt-1",
                "invoice.paid",
                "abc123",
                "req-1",
                local,
                Recibido));
    }

    [Fact]
    public void CrearVerificado_NormalizaContratoSinGuardarFirmaNiSecreto()
    {
        var webhook = WebhookEntrante.CrearVerificado(
            7,
            " Stripe ",
            " evt-123 ",
            " invoice.paid ",
            " ABCDEF123456 ",
            " req-123 ",
            Emitido,
            Recibido);

        Assert.Equal(7, webhook.EmpresaId);
        Assert.Equal("Stripe", webhook.Proveedor);
        Assert.Equal("evt-123", webhook.EventoExternoId);
        Assert.Equal("invoice.paid", webhook.TipoEvento);
        Assert.Equal("ABCDEF123456", webhook.PayloadHash);
        Assert.Equal("req-123", webhook.CorrelationId);
        Assert.Equal(Emitido, webhook.EmitidoEnUtc);
        Assert.Equal(Recibido, webhook.RecibidoEnUtc);
        Assert.Equal(EstadoWebhookEntrante.Recibido, webhook.Estado);
        Assert.False(webhook.EsTerminal);

        var properties = typeof(WebhookEntrante).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(properties, name => name.Contains("Firma", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, name => name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CoincidePayload_PermiteReplayIdenticoYDistingueConflicto()
    {
        var webhook = CrearValido();

        Assert.True(webhook.CoincidePayload("abcdef123456"));
        Assert.True(webhook.CoincidePayload(" ABCDEF123456 "));
        Assert.False(webhook.CoincidePayload("999999"));
    }

    [Fact]
    public void Procesar_CompletaUnaSolaVezYQuedaTerminal()
    {
        var webhook = CrearValido();
        var inicio = Recibido.AddSeconds(1);
        var fin = inicio.AddSeconds(2);

        webhook.MarcarProcesando(inicio);
        webhook.MarcarProcesado(fin);

        Assert.Equal(EstadoWebhookEntrante.Procesado, webhook.Estado);
        Assert.Equal(fin, webhook.ProcesadoEnUtc);
        Assert.Null(webhook.ProcesandoDesdeUtc);
        Assert.True(webhook.EsTerminal);
        Assert.Throws<InvalidOperationException>(() => webhook.MarcarProcesando(fin.AddSeconds(1)));
        Assert.Throws<InvalidOperationException>(() => webhook.MarcarRechazado("duplicado", fin.AddSeconds(1)));
    }

    [Fact]
    public void Rechazar_NormalizaMotivoYBloqueaReprocesamiento()
    {
        var webhook = CrearValido();
        var rechazo = Recibido.AddSeconds(1);

        webhook.MarcarRechazado(" replay detectado ", rechazo);

        Assert.Equal(EstadoWebhookEntrante.Rechazado, webhook.Estado);
        Assert.Equal("replay detectado", webhook.MotivoRechazo);
        Assert.Equal(rechazo, webhook.RechazadoEnUtc);
        Assert.True(webhook.EsTerminal);
        Assert.Throws<InvalidOperationException>(() => webhook.MarcarProcesando(rechazo.AddSeconds(1)));
    }

    private static WebhookEntrante CrearValido() =>
        WebhookEntrante.CrearVerificado(
            7,
            "Stripe",
            "evt-123",
            "invoice.paid",
            "ABCDEF123456",
            "req-123",
            Emitido,
            Recibido);
}
