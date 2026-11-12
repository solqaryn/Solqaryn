using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-004.F: prepara un caso negativo reproducible de correo de recuperacion.
/// Un remitente configurado con sintaxis invalida debe bloquear diagnostico y
/// entrega antes de intentar SMTP. Solo datos sinteticos, sin correo real.
/// No prueba entrega, bloqueo de enlaces PROD ni expiracion de tokens.
/// </summary>
public sealed class I8004FPrepararCasoCorreoSeguroTests
{
    [Fact]
    public async Task RemitenteInvalido_NoAbreTransporteNiExponeDatosDelMensaje()
    {
        const string tokenSintetico = "refresh-i8004f-fixture-sin-credencial-real";
        const string enlaceSintetico = "https://prod.invalid/reset?token=solo-prueba-i8004f";
        var opciones = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp-mail.outlook.com",
            ["Smtp:Port"] = "587",
            ["Smtp:UsuarioSmtp"] = "emisor@dev.test",
            ["Smtp:CorreoRemitente"] = "remitente@@dev.test",
            ["Smtp:OAuth2ClientId"] = "oauth-cliente-sintetico-i8004f",
            ["Smtp:OAuth2RefreshToken"] = tokenSintetico,
            ["Smtp:UsarSsl"] = "true",
            ["Smtp:RequiereAutenticacion"] = "true"
        };
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(opciones).Build();
        var servicio = new SmtpEmailService(
            configuracion, NullLogger<SmtpEmailService>.Instance);

        var estado = servicio.ObtenerEstadoConfiguracion();
        Assert.False(estado.Configurado);
        Assert.Contains("remitente", estado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(tokenSintetico, estado.Mensaje, StringComparison.Ordinal);

        var diagnostico = await servicio.ProbarConexionAsync();
        Assert.False(diagnostico.Exito);
        Assert.Equal("SMTP_NO_CONFIGURADO", diagnostico.Codigo);
        Assert.False(diagnostico.Autenticado);

        var entrega = await servicio.EnviarAsync(
            "destinatario@dev.test", "Recuperacion individual",
            "<p>Mensaje de prueba, no enviar.</p><a href=\"" + enlaceSintetico +
            "\">Enlace sintetico</a>");

        Assert.False(entrega.Exito);
        Assert.Equal("SMTP_NO_CONFIGURADO", entrega.Codigo);
        Assert.Equal(0, entrega.Intentos);
        Assert.Null(entrega.MessageId);
        Assert.DoesNotContain(tokenSintetico, entrega.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(enlaceSintetico, entrega.Error ?? string.Empty, StringComparison.Ordinal);
    }
}
