using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-004.D: el flujo de correo seguro no conecta ni envia si el transporte
/// Outlook no usa el puerto STARTTLS exigido o Reply-To es invalido.
/// Todas las credenciales de esta prueba son sinteticas.
/// </summary>
public sealed class I8004DFlujoCorreoSeguroTests
{
    private const string TokenDePrueba = "refresh-sintetico-i8004d-no-secreto";

    [Theory]
    [InlineData("465", null, "587")]
    [InlineData("587", "respuesta sin arroba", "respuesta")]
    public async Task TransporteInseguroORespuestaInvalida_SeRechazaAntesDeConectar(
        string puerto,
        string? correoRespuesta,
        string fragmentoError)
    {
        var servicio = CrearServicio(puerto, correoRespuesta);
        var estado = servicio.ObtenerEstadoConfiguracion();

        Assert.False(estado.Configurado);
        Assert.Contains(fragmentoError, estado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(TokenDePrueba, estado.Mensaje, StringComparison.Ordinal);

        var diagnostico = await servicio.ProbarConexionAsync();
        Assert.False(diagnostico.Exito);
        Assert.Equal("SMTP_NO_CONFIGURADO", diagnostico.Codigo);
        Assert.False(diagnostico.Autenticado);

        var entrega = await servicio.EnviarAsync(
            "cliente@dev.test",
            "Recuperacion de cuenta",
            "<p>Mensaje sintetico sin enlaces ni credenciales.</p>");

        Assert.False(entrega.Exito);
        Assert.Equal("SMTP_NO_CONFIGURADO", entrega.Codigo);
        Assert.Equal(0, entrega.Intentos);
        Assert.Null(entrega.MessageId);
        Assert.DoesNotContain(TokenDePrueba, entrega.Error ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfiguracionValida_OutlookExigeStartTlsSinConectar()
    {
        var estado = CrearServicio("587", "respuesta@dev.test").ObtenerEstadoConfiguracion();

        Assert.True(estado.Configurado, estado.Mensaje);
        Assert.True(estado.RequiereAutenticacion);
        Assert.True(estado.UsaTls);
        Assert.Equal("STARTTLS obligatorio", estado.ModoSeguridad);
        Assert.DoesNotContain(TokenDePrueba, estado.Mensaje, StringComparison.Ordinal);
    }

    private static SmtpEmailService CrearServicio(string puerto, string? respuesta)
    {
        var valores = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp-mail.outlook.com",
            ["Smtp:Port"] = puerto,
            ["Smtp:UsuarioSmtp"] = "emisor@dev.test",
            ["Smtp:CorreoRemitente"] = "emisor@dev.test",
            ["Smtp:CorreoRespuesta"] = respuesta,
            ["Smtp:OAuth2ClientId"] = "cliente-oauth-sintetico-no-secreto",
            ["Smtp:OAuth2RefreshToken"] = TokenDePrueba,
            ["Smtp:UsarSsl"] = "true",
            ["Smtp:RequiereAutenticacion"] = "true"
        };

        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new SmtpEmailService(configuracion, NullLogger<SmtpEmailService>.Instance);
    }
}
