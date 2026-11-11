using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-004.E: prueba negativa offline del transporte de correo de recuperacion.
/// Verifica rechazo previo a red para un host placeholder o un mensaje mal formado.
/// No acredita que el contenido sea validado contra dominios PROD ni que se
/// impidan contrasenas compartidas con SMTP correctamente configurado.
/// </summary>
public sealed class I8004EFlujoCorreoSeguroTests
{
    private const string RefreshSintetico = "refresh-sintetico-i8004e-no-secreto";
    private const string MarcadorCuerpo = "marcador-sintetico-no-credencial-i8004e";
    private const string CuerpoSintetico =
        "<p>PRUEBA: no enviar clave generica ni enlace a produccion.</p>" +
        "<a href=\"https://prod.invalid/reset?token=" + MarcadorCuerpo +
        "\">Enlace sintetico inseguro (no real)</a>";

    [Theory]
    [InlineData("CHANGE_ME")]
    [InlineData("REPLACE_ME")]
    public async Task I8004E_HostPlaceholder_NoEnviaMensajeDeRecuperacion(string host)
    {
        var servicio = CrearServicio(host);
        var estado = servicio.ObtenerEstadoConfiguracion();

        Assert.False(estado.Configurado);
        Assert.Equal("No configurado", estado.Host);
        Assert.DoesNotContain(RefreshSintetico, estado.Mensaje, StringComparison.Ordinal);

        var resultado = await servicio.EnviarAsync(
            "cliente@dev.test", "Recuperacion de cuenta", CuerpoSintetico);

        Assert.False(resultado.Exito);
        Assert.Equal("SMTP_NO_CONFIGURADO", resultado.Codigo);
        Assert.Equal(0, resultado.Intentos);
        Assert.Null(resultado.MessageId);
        Assert.DoesNotContain(RefreshSintetico, resultado.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcadorCuerpo, resultado.Error ?? string.Empty, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("destinatario@@dev.test", "Recuperacion de cuenta", "DESTINATARIO_INVALIDO")]
    [InlineData("cliente@dev.test", " \r\n ", "ASUNTO_INVALIDO")]
    public async Task I8004E_MensajeInvalido_NoIniciaTransporte(
        string destinatario, string asunto, string codigo)
    {
        // MimeKit puede aceptar una direccion solo-local; usar doble @ garantiza\n        // sintaxis invalida antes de OAuth2 y sin tocar el validador productivo.\n        // Credenciales y dominio exclusivamente sinteticos: la llamada debe
        // fallar ANTES de abrir ninguna conexion SMTP.
        var servicio = CrearServicio("smtp-mail.outlook.com");
        Assert.True(servicio.ObtenerEstadoConfiguracion().Configurado);

        var resultado = await servicio.EnviarAsync(
            destinatario, asunto, CuerpoSintetico);

        Assert.False(resultado.Exito);
        Assert.Equal(codigo, resultado.Codigo);
        Assert.Equal(0, resultado.Intentos);
        Assert.Null(resultado.MessageId);
        Assert.DoesNotContain(RefreshSintetico, resultado.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcadorCuerpo, resultado.Error ?? string.Empty, StringComparison.Ordinal);
    }

    private static SmtpEmailService CrearServicio(string host)
    {
        var valores = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = host,
            ["Smtp:Port"] = "587",
            ["Smtp:UsuarioSmtp"] = "emisor@dev.test",
            ["Smtp:CorreoRemitente"] = "emisor@dev.test",
            ["Smtp:OAuth2ClientId"] = "cliente-oauth-sintetico-i8004e",
            ["Smtp:OAuth2RefreshToken"] = RefreshSintetico,
            ["Smtp:UsarSsl"] = "true",
            ["Smtp:RequiereAutenticacion"] = "true"
        };

        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new SmtpEmailService(configuracion, NullLogger<SmtpEmailService>.Instance);
    }
}
