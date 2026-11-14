using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class EmailEmpresarialDomainTests
{
    [Fact]
    public void CrearDirecto_FijaTenantIdempotenciaYEstadoPendiente()
    {
        var ahora = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc);

        var email = EmailEmpresarial.CrearDirecto(
            empresaId: 7,
            destinatario: " cliente@example.com ",
            asunto: "Factura lista",
            cuerpoHtml: "<p>Lista</p>",
            claveIdempotencia: "factura:99:email:v1",
            correlationId: "corr-99",
            creadoEnUtc: ahora);

        Assert.Equal(7, email.EmpresaId);
        Assert.Equal("cliente@example.com", email.Destinatario);
        Assert.Equal("factura:99:email:v1", email.ClaveIdempotencia);
        Assert.Equal("corr-99", email.CorrelationId);
        Assert.Equal(EstadoEntregaEmail.Pendiente, email.Estado);
        Assert.Equal(ahora, email.DisponibleDesdeUtc);
        Assert.False(email.EsDespachoTerminal);
    }

    [Fact]
    public void CrearDesdePlantilla_FijaCodigoVersionYVariablesSinMaterializarSecretos()
    {
        var email = EmailEmpresarial.CrearDesdePlantilla(
            empresaId: 3,
            destinatario: "cliente@example.com",
            plantillaCodigo: "FACTURA_EMITIDA",
            plantillaVersion: 4,
            variablesJson: "{\"numero\":\"F-001\"}",
            claveIdempotencia: "email-123");

        Assert.Equal("FACTURA_EMITIDA", email.PlantillaCodigo);
        Assert.Equal(4, email.PlantillaVersion);
        Assert.Equal("{\"numero\":\"F-001\"}", email.VariablesJson);
        Assert.Null(email.Asunto);
        Assert.Null(email.CuerpoHtml);
    }

    [Fact]
    public void RegistrarFallo_ProgramaReintentoYAlAgotarQuedaFallidoFinal()
    {
        var creado = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc);
        var email = EmailEmpresarial.CrearDirecto(
            1,
            "cliente@example.com",
            "Asunto",
            "<p>Contenido</p>",
            "idem-1",
            creadoEnUtc: creado);

        email.MarcarProcesando(creado);
        email.RegistrarFallo(
            "timeout",
            creado.AddMinutes(1),
            creado.AddMinutes(5),
            maximoIntentos: 2);

        Assert.Equal(EstadoEntregaEmail.ReintentoPendiente, email.Estado);
        Assert.Equal(creado.AddMinutes(5), email.DisponibleDesdeUtc);
        Assert.Throws<InvalidOperationException>(() => email.MarcarProcesando(creado.AddMinutes(4)));

        email.MarcarProcesando(creado.AddMinutes(5));
        email.RegistrarFallo(
            "timeout otra vez",
            creado.AddMinutes(6),
            creado.AddMinutes(10),
            maximoIntentos: 2);

        Assert.Equal(EstadoEntregaEmail.FallidoFinal, email.Estado);
        Assert.True(email.EsDespachoTerminal);
        Assert.True(email.EsEntregaTerminal);
        Assert.Equal(2, email.Intentos);
    }

    [Fact]
    public void AceptadoProveedor_PuedeConfirmarEntregaConCorrelationDurable()
    {
        var creado = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc);
        var email = EmailEmpresarial.CrearDirecto(
            2,
            "cliente@example.com",
            "Asunto",
            "<p>Contenido</p>",
            "idem-2",
            creadoEnUtc: creado);

        email.MarcarProcesando(creado);
        email.MarcarAceptadoProveedor("provider-message-42", creado.AddSeconds(5));

        Assert.Equal(EstadoEntregaEmail.AceptadoProveedor, email.Estado);
        Assert.Equal("provider-message-42", email.ProviderMessageId);
        Assert.True(email.EsDespachoTerminal);
        Assert.False(email.EsEntregaTerminal);

        email.MarcarEntregado(creado.AddMinutes(1));

        Assert.Equal(EstadoEntregaEmail.Entregado, email.Estado);
        Assert.True(email.EsEntregaTerminal);
        Assert.Equal(creado.AddMinutes(1), email.EntregadoEnUtc);
    }

    [Fact]
    public void Rebote_SoloSeRegistraDespuesDeAceptacionDelProveedor()
    {
        var creado = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc);
        var email = EmailEmpresarial.CrearDirecto(
            2,
            "cliente@example.com",
            "Asunto",
            "<p>Contenido</p>",
            "idem-bounce",
            creadoEnUtc: creado);

        Assert.Throws<InvalidOperationException>(() =>
            email.RegistrarRebote(
                TipoReboteEmail.Permanente,
                "550",
                "Mailbox unavailable",
                creado.AddMinutes(1)));

        email.MarcarProcesando(creado);
        email.MarcarAceptadoProveedor("provider-bounce-1", creado.AddSeconds(2));
        email.RegistrarRebote(
            TipoReboteEmail.Permanente,
            "550",
            "Mailbox unavailable",
            creado.AddMinutes(1));

        Assert.Equal(EstadoEntregaEmail.Rebotado, email.Estado);
        Assert.Equal(TipoReboteEmail.Permanente, email.TipoRebote);
        Assert.Equal("550", email.CodigoRebote);
        Assert.Equal("Mailbox unavailable", email.UltimoError);
        Assert.True(email.EsEntregaTerminal);
    }

    [Fact]
    public void Plantilla_ConservaVersionTenantYContenidoInmutableAlDesactivar()
    {
        var creado = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc);
        var plantilla = PlantillaEmailEmpresarial.Crear(
            empresaId: 9,
            codigo: "COBRO_VENCIDO",
            version: 2,
            nombre: "Cobro vencido",
            asuntoPlantilla: "Pago pendiente {{numero}}",
            cuerpoHtmlPlantilla: "<p>{{total}}</p>",
            creadoEnUtc: creado);

        Assert.Equal(9, plantilla.EmpresaId);
        Assert.Equal("COBRO_VENCIDO", plantilla.Codigo);
        Assert.Equal(2, plantilla.Version);
        Assert.True(plantilla.Activa);

        plantilla.Desactivar(creado.AddMinutes(2));

        Assert.False(plantilla.Activa);
        Assert.Equal("Pago pendiente {{numero}}", plantilla.AsuntoPlantilla);
        Assert.Equal("<p>{{total}}</p>", plantilla.CuerpoHtmlPlantilla);
    }

    [Fact]
    public void Contratos_RechazanTenantVersionYFechasInvalidas()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EmailEmpresarial.CrearDirecto(
            0,
            "cliente@example.com",
            "Asunto",
            "<p>Contenido</p>",
            "idem"));

        Assert.Throws<ArgumentOutOfRangeException>(() => EmailEmpresarial.CrearDesdePlantilla(
            1,
            "cliente@example.com",
            "PLANTILLA",
            0,
            "{}",
            "idem"));

        Assert.Throws<ArgumentException>(() => EmailEmpresarial.CrearDirecto(
            1,
            "cliente@example.com",
            "Asunto",
            "<p>Contenido</p>",
            "idem",
            creadoEnUtc: new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Local)));
    }
}
