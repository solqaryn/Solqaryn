using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-004.C: un correo de recuperación no puede intentar transporte SMTP
/// sin la credencial OAuth2 exigida. Prueba aislada: no abre conexiones.
/// </summary>
public sealed class I8004CCorreoSeguroTests
{
    [Fact]
    public async Task RecuperacionSinRefreshToken_NoEnviaNiRevelaCredenciales()
    {
        const string marcadorCredencial = "oauth-client-marcador-no-secreto";
        var valores = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp-mail.outlook.com",
            ["Smtp:Port"] = "587",
            ["Smtp:UsuarioSmtp"] = "emisor@dev.test",
            ["Smtp:CorreoRemitente"] = "emisor@dev.test",
            ["Smtp:OAuth2ClientId"] = marcadorCredencial,
            ["Smtp:OAuth2RefreshToken"] = "",
            ["Smtp:UsarSsl"] = "true",
            ["Smtp:RequiereAutenticacion"] = "true"
        };

        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var servicio = new SmtpEmailService(configuracion, NullLogger<SmtpEmailService>.Instance);

        var resultado = await servicio.EnviarAsync(
            "destinatario@dev.test",
            "Recuperación de cuenta",
            "<p>Prueba local sin enlace ni clave compartida.</p>");

        Assert.False(resultado.Exito);
        Assert.Equal("SMTP_NO_CONFIGURADO", resultado.Codigo);
        Assert.Equal(0, resultado.Intentos);
        Assert.Null(resultado.MessageId);
        Assert.DoesNotContain(marcadorCredencial, resultado.Error ?? string.Empty, StringComparison.Ordinal);
    }
}
