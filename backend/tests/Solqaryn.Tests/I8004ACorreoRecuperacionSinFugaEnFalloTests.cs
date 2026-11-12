using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-004.A: recibo negativo del flujo SMTP de recuperacion ante una credencial
/// OAuth2 ausente. Un cuerpo adversarial no debe llegar a transporte ni a logs.
/// No prueba enlaces con SMTP correctamente configurado ni el host DEV/PROD real.
/// </summary>
public sealed class I8004ACorreoRecuperacionSinFugaEnFalloTests
{
    private const string MarcadorToken = "token-restablecimiento-sintetico-i8004a";
    private const string MarcadorClave = "clave-generica-sintetica-i8004a";

    [Theory]
    [InlineData("<a href='https://prod.invalid/reset?token=token-restablecimiento-sintetico-i8004a'>Recuperar</a>")]
    [InlineData("<p>Clave comun: clave-generica-sintetica-i8004a</p>")]
    public async Task RecuperacionSinRefreshOAuth_NoConectaNiFiltraContenidoEnLog(string cuerpoHtml)
    {
        var logger = new CapturaLog();
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["Smtp:Host"] = "smtp-mail.outlook.com",
                ["Smtp:Port"] = "587",
                ["Smtp:UsuarioSmtp"] = "emisor@dev.test",
                ["Smtp:CorreoRemitente"] = "emisor@dev.test",
                ["Smtp:OAuth2ClientId"] = "cliente-oauth-sintetico-i8004a",
                ["Smtp:OAuth2RefreshToken"] = "",
                ["Smtp:UsarSsl"] = "true",
                ["Smtp:RequiereAutenticacion"] = "true"
            })
            .Build();
        var servicio = new SmtpEmailService(configuracion, logger);

        var estado = servicio.ObtenerEstadoConfiguracion();
        Assert.False(estado.Configurado);
        Assert.DoesNotContain(MarcadorToken, estado.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcadorClave, estado.Mensaje, StringComparison.Ordinal);

        var entrega = await servicio.EnviarAsync(
            "destinatario@dev.test",
            "Recuperacion de cuenta",
            cuerpoHtml);

        Assert.False(entrega.Exito);
        Assert.Equal("SMTP_NO_CONFIGURADO", entrega.Codigo);
        Assert.Equal(0, entrega.Intentos);
        Assert.Null(entrega.MessageId);

        var logs = string.Join("\n", logger.Mensajes);
        Assert.Contains("SMTP no disponible", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcadorToken, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcadorClave, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("prod.invalid", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcadorToken, entrega.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(MarcadorClave, entrega.Error ?? string.Empty, StringComparison.Ordinal);
    }

    private sealed class CapturaLog : ILogger<SmtpEmailService>
    {
        public List<string> Mensajes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Mensajes.Add(formatter(state, exception));
        }
    }
}
