using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text.Json;
using InventoryApp.Application.Interfaces;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Utils;
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace InventoryApp.Infrastructure.Services;

/// Envío SMTP transaccional para facturas. Usa MailKit para negociar STARTTLS
/// de forma explícita en el puerto 587, SSL directo en 465 y errores SMTP más
/// precisos. Los certificados se validan con el almacén de confianza del sistema.
public sealed class SmtpEmailService : IEmailService
{
    private const int MaximoAdjuntos = 5;
    private const int MaximoTotalAdjuntosBytes = 20 * 1024 * 1024;
    private const string OAuth2TokenEndpointPredeterminado = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    private const string OAuth2ScopePredeterminado = "https://outlook.office.com/SMTP.Send offline_access";
    private static readonly HttpClient OAuthHttpClient = new();

    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public EstadoConfiguracionSmtp ObtenerEstadoConfiguracion()
    {
        var configuracion = LeerConfiguracion();
        var validacion = ValidarConfiguracion(configuracion);

        return new EstadoConfiguracionSmtp
        {
            Configurado = validacion.Error is null,
            Host = EnmascararHost(configuracion.Host),
            Puerto = configuracion.Puerto,
            UsaTls = configuracion.UsarSsl,
            ModoSeguridad = ObtenerNombreSeguridad(configuracion),
            RequiereAutenticacion = configuracion.RequiereAutenticacion,
            RemitenteEnmascarado = EnmascararCorreo(configuracion.CorreoRemitente),
            MaximoIntentos = configuracion.MaximoIntentos,
            TimeoutSegundos = configuracion.TimeoutSegundos,
            Mensaje = validacion.Error ??
                "Configuración válida. Falta comprobar conexión, negociación TLS y autenticación con el servidor SMTP."
        };
    }

    public async Task<ResultadoDiagnosticoSmtp> ProbarConexionAsync(
        CancellationToken cancellationToken = default)
    {
        var configuracion = LeerConfiguracion();
        var validacion = ValidarConfiguracion(configuracion);
        var cronometro = Stopwatch.StartNew();

        if (validacion.Error is not null)
        {
            return CrearDiagnostico(
                false,
                "SMTP_NO_CONFIGURADO",
                validacion.Error,
                configuracion,
                false,
                cronometro.ElapsedMilliseconds);
        }

        try
        {
            using var cliente = CrearCliente(configuracion);
            await EjecutarConTimeoutAsync(
                async token =>
                {
                    await ConectarYAutenticarAsync(cliente, configuracion, token);
                    await cliente.NoOpAsync(token);
                    await cliente.DisconnectAsync(true, token);
                },
                configuracion.TimeoutSegundos,
                cancellationToken);

            cronometro.Stop();
            _logger.LogInformation(
                "Diagnóstico SMTP aprobado para {Host}:{Puerto}; Seguridad={Seguridad}; Autenticacion={Autenticacion}; DuracionMs={DuracionMs}.",
                configuracion.Host,
                configuracion.Puerto,
                ObtenerNombreSeguridad(configuracion),
                configuracion.RequiereAutenticacion,
                cronometro.ElapsedMilliseconds);

            return CrearDiagnostico(
                true,
                "SMTP_OK",
                configuracion.RequiereAutenticacion
                    ? "Conexión, TLS y autenticación SMTP comprobados correctamente."
                    : "Conexión SMTP comprobada correctamente; autenticación deshabilitada para este entorno.",
                configuracion,
                configuracion.RequiereAutenticacion,
                cronometro.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            cronometro.Stop();
            var (codigo, mensaje, _) = MapearError(ex, configuracion);

            _logger.LogError(
                ex,
                "Diagnóstico SMTP fallido. Codigo={Codigo}; Host={Host}; Puerto={Puerto}; Seguridad={Seguridad}; DuracionMs={DuracionMs}.",
                codigo,
                configuracion.Host,
                configuracion.Puerto,
                ObtenerNombreSeguridad(configuracion),
                cronometro.ElapsedMilliseconds);

            return CrearDiagnostico(
                false,
                codigo,
                mensaje,
                configuracion,
                false,
                cronometro.ElapsedMilliseconds);
        }
    }

    public async Task<ResultadoEntregaEmail> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<AdjuntoCorreo>? adjuntos = null,
        CancellationToken cancellationToken = default)
    {
        var configuracion = LeerConfiguracion();
        var validacion = ValidarConfiguracion(configuracion);
        if (validacion.Error is not null)
        {
            _logger.LogWarning("SMTP no disponible: {Motivo}", validacion.Error);
            return Fallo("SMTP_NO_CONFIGURADO", validacion.Error, false, 0);
        }

        if (!MailboxAddress.TryParse(destinatario?.Trim(), out var direccionDestino))
            return Fallo("DESTINATARIO_INVALIDO", "La dirección de correo del destinatario no es válida.", false, 0);

        asunto = LimpiarEncabezado(asunto);
        if (string.IsNullOrWhiteSpace(asunto))
            return Fallo("ASUNTO_INVALIDO", "El asunto del correo es obligatorio.", false, 0);

        cuerpoHtml ??= string.Empty;
        adjuntos ??= new List<AdjuntoCorreo>();
        var errorAdjuntos = ValidarAdjuntos(adjuntos);
        if (errorAdjuntos is not null)
            return Fallo("ADJUNTOS_INVALIDOS", errorAdjuntos, false, 0);

        var messageId = $"solqaryn-{Guid.NewGuid():N}";
        Exception? ultimaExcepcion = null;

        for (var intento = 1; intento <= configuracion.MaximoIntentos; intento++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var mensaje = CrearMensaje(
                    configuracion,
                    direccionDestino,
                    asunto,
                    cuerpoHtml,
                    adjuntos,
                    messageId);
                using var cliente = CrearCliente(configuracion);

                _logger.LogInformation(
                    "Intento SMTP {Intento}/{MaximoIntentos} para {DestinatarioEnmascarado} mediante {Host}:{Puerto}; Seguridad={Seguridad}; MessageId={MessageId}.",
                    intento,
                    configuracion.MaximoIntentos,
                    EnmascararCorreo(direccionDestino.Address),
                    configuracion.Host,
                    configuracion.Puerto,
                    ObtenerNombreSeguridad(configuracion),
                    messageId);

                await EjecutarConTimeoutAsync(
                    async token =>
                    {
                        await ConectarYAutenticarAsync(cliente, configuracion, token);
                        await cliente.SendAsync(mensaje, token);
                        await cliente.DisconnectAsync(true, token);
                    },
                    configuracion.TimeoutSegundos,
                    cancellationToken);

                _logger.LogInformation(
                    "Correo enviado a {DestinatarioEnmascarado} en {Intentos} intento(s). MessageId={MessageId}.",
                    EnmascararCorreo(direccionDestino.Address),
                    intento,
                    messageId);

                return new ResultadoEntregaEmail
                {
                    Exito = true,
                    Codigo = "ENVIADO",
                    Intentos = intento,
                    MessageId = messageId
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Envío SMTP cancelado para {DestinatarioEnmascarado}. MessageId={MessageId}.",
                    EnmascararCorreo(direccionDestino.Address),
                    messageId);
                throw;
            }
            catch (Exception ex) when (EsErrorTransitorio(ex) && intento < configuracion.MaximoIntentos)
            {
                ultimaExcepcion = ex;
                var demora = CalcularDemora(configuracion.RetryBaseDelayMilliseconds, intento);
                _logger.LogWarning(
                    ex,
                    "Fallo SMTP transitorio en intento {Intento}/{MaximoIntentos}; reintento en {DemoraMs} ms. Destinatario={DestinatarioEnmascarado}; MessageId={MessageId}.",
                    intento,
                    configuracion.MaximoIntentos,
                    demora.TotalMilliseconds,
                    EnmascararCorreo(direccionDestino.Address),
                    messageId);
                await Task.Delay(demora, cancellationToken);
            }
            catch (Exception ex)
            {
                ultimaExcepcion = ex;
                return CrearFalloTecnico(ex, direccionDestino.Address, configuracion, intento, messageId);
            }
        }

        return CrearFalloTecnico(
            ultimaExcepcion ?? new TimeoutException("El servidor SMTP no respondió."),
            direccionDestino.Address,
            configuracion,
            configuracion.MaximoIntentos,
            messageId);
    }

    private ConfiguracionSmtp LeerConfiguracion()
    {
        var usuario = _configuration["Smtp:UsuarioSmtp"]?.Trim() ?? string.Empty;
        var remitente = _configuration["Smtp:CorreoRemitente"]?.Trim();
        if (string.IsNullOrWhiteSpace(remitente)) remitente = usuario;

        return new ConfiguracionSmtp(
            Host: _configuration["Smtp:Host"]?.Trim() ?? string.Empty,
            Puerto: _configuration.GetValue<int?>("Smtp:Port") ?? 587,
            Usuario: usuario,
            OAuth2ClientId: _configuration["Smtp:OAuth2ClientId"]?.Trim() ?? string.Empty,
            OAuth2RefreshToken: _configuration["Smtp:OAuth2RefreshToken"]?.Trim() ?? string.Empty,
            UsarSsl: _configuration.GetValue<bool?>("Smtp:UsarSsl") ?? true,
            RequiereAutenticacion: _configuration.GetValue<bool?>("Smtp:RequiereAutenticacion") ?? true,
            CorreoRemitente: remitente ?? string.Empty,
            NombreRemitente: LimpiarEncabezado(_configuration["Smtp:NombreRemitente"] ?? "SOLQARYN"),
            CorreoRespuesta: _configuration["Smtp:CorreoRespuesta"]?.Trim(),
            TimeoutSegundos: Math.Clamp(_configuration.GetValue<int?>("Smtp:TimeoutSeconds") ?? 60, 10, 300),
            MaximoIntentos: Math.Clamp(_configuration.GetValue<int?>("Smtp:MaxAttempts") ?? 3, 1, 5),
            RetryBaseDelayMilliseconds: Math.Clamp(_configuration.GetValue<int?>("Smtp:RetryBaseDelayMilliseconds") ?? 500, 50, 10_000));
    }

    private static (string? Error, MailboxAddress? Remitente) ValidarConfiguracion(ConfiguracionSmtp configuracion)
    {
        if (string.IsNullOrWhiteSpace(configuracion.Host) || EsPlaceholder(configuracion.Host))
            return ("El host SMTP del entorno actual no está configurado.", null);

        if (configuracion.Puerto is < 1 or > 65535)
            return ("El puerto SMTP configurado no es válido.", null);

        if (configuracion.RequiereAutenticacion)
        {
            if (string.IsNullOrWhiteSpace(configuracion.Usuario) || EsPlaceholder(configuracion.Usuario))
                return ("El usuario SMTP del entorno actual no está configurado.", null);
            if (string.IsNullOrWhiteSpace(configuracion.OAuth2ClientId) || EsPlaceholder(configuracion.OAuth2ClientId))
                return ("El Client ID OAuth2 de SMTP no está configurado.", null);

            if (string.IsNullOrWhiteSpace(configuracion.OAuth2RefreshToken) || EsPlaceholder(configuracion.OAuth2RefreshToken))
                return ("El refresh token OAuth2 de SMTP no está configurado.", null);
        }

        if (!MailboxAddress.TryParse(configuracion.CorreoRemitente, out var remitente))
            return ("El correo remitente SMTP configurado no es válido.", null);

        if (!string.IsNullOrWhiteSpace(configuracion.CorreoRespuesta) &&
            !MailboxAddress.TryParse(configuracion.CorreoRespuesta, out _))
        {
            return ("El correo de respuesta SMTP configurado no es válido.", null);
        }

        if (EsOutlook(configuracion.Host) && configuracion.Puerto != 587)
            return ("Para Outlook.com usa el puerto 587 con STARTTLS.", null);

        return (null, remitente);
    }

    private static string? ValidarAdjuntos(List<AdjuntoCorreo> adjuntos)
    {
        if (adjuntos.Count > MaximoAdjuntos)
            return $"El correo no puede incluir más de {MaximoAdjuntos} archivos adjuntos.";

        if (adjuntos.Any(a => a.Contenido is not { Length: > 0 }))
            return "Todos los archivos adjuntos deben contener datos.";

        var totalAdjuntos = adjuntos.Sum(a => (long)a.Contenido.Length);
        if (totalAdjuntos > MaximoTotalAdjuntosBytes)
            return "Los archivos adjuntos superan el límite total permitido de 20 MB.";

        return null;
    }

    private static MimeMessage CrearMensaje(
        ConfiguracionSmtp configuracion,
        MailboxAddress destino,
        string asunto,
        string cuerpoHtml,
        List<AdjuntoCorreo> adjuntos,
        string messageId)
    {
        var validacion = ValidarConfiguracion(configuracion);
        var remitente = validacion.Remitente
            ?? throw new InvalidOperationException(validacion.Error ?? "Configuración SMTP inválida.");

        var mensaje = new MimeMessage
        {
            Subject = asunto,
            MessageId = MimeUtils.GenerateMessageId("solqaryn.local")
        };

        mensaje.From.Add(new MailboxAddress(configuracion.NombreRemitente, remitente.Address));
        mensaje.To.Add(destino);

        var respuesta = string.IsNullOrWhiteSpace(configuracion.CorreoRespuesta)
            ? remitente.Address
            : configuracion.CorreoRespuesta;
        mensaje.ReplyTo.Add(new MailboxAddress(configuracion.NombreRemitente, respuesta));
        mensaje.Headers.Add("X-Solqaryn-Message-Id", messageId);
        mensaje.Headers.Add("X-Auto-Response-Suppress", "All");

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = cuerpoHtml,
            TextBody = ConvertirHtmlATexto(cuerpoHtml)
        };

        foreach (var adjunto in adjuntos)
        {
            var nombreArchivo = LimpiarNombreArchivo(adjunto.NombreArchivo);
            var contentType = CrearContentType(adjunto.ContentType);
            bodyBuilder.Attachments.Add(nombreArchivo, adjunto.Contenido, contentType);
        }

        mensaje.Body = bodyBuilder.ToMessageBody();
        return mensaje;
    }

    private static ContentType CrearContentType(string? valor)
    {
        var partes = (valor ?? string.Empty).Trim().Split('/', 2, StringSplitOptions.TrimEntries);
        return partes.Length == 2 && partes.All(p => !string.IsNullOrWhiteSpace(p))
            ? new ContentType(partes[0], partes[1])
            : new ContentType("application", "octet-stream");
    }

    private static MailKitSmtpClient CrearCliente(ConfiguracionSmtp configuracion) => new()
    {
        Timeout = configuracion.TimeoutSegundos * 1000,
        CheckCertificateRevocation = true
    };

    private static async Task ConectarYAutenticarAsync(
        MailKitSmtpClient cliente,
        ConfiguracionSmtp configuracion,
        CancellationToken cancellationToken)
    {
        await cliente.ConnectAsync(
            configuracion.Host,
            configuracion.Puerto,
            ResolverSeguridad(configuracion),
            cancellationToken);

        if (!configuracion.RequiereAutenticacion)
            return;
        var accessToken = await ObtenerAccessTokenOAuth2Async(configuracion, cancellationToken);
        var oauth2 = new SaslMechanismOAuth2(configuracion.Usuario, accessToken);
        await cliente.AuthenticateAsync(oauth2, cancellationToken);
    }

    private static async Task<string> ObtenerAccessTokenOAuth2Async(
        ConfiguracionSmtp configuracion,
        CancellationToken cancellationToken)
    {
        var campos = new List<KeyValuePair<string, string>>
        {
            new("client_id", configuracion.OAuth2ClientId),
            new("grant_type", "refresh_token"),
            new("refresh_token", configuracion.OAuth2RefreshToken),
            new("scope", OAuth2ScopePredeterminado)
        };


        using var request = new HttpRequestMessage(HttpMethod.Post, OAuth2TokenEndpointPredeterminado)
        {
            Content = new FormUrlEncodedContent(campos)
        };

        using var response = await OAuthHttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new SmtpOAuth2Exception(
                $"Microsoft OAuth2 rechazó la renovación del token SMTP (HTTP {(int)response.StatusCode}).");
        }

        try
        {
            using var json = JsonDocument.Parse(payload);
            if (!json.RootElement.TryGetProperty("access_token", out var accessTokenElement))
                throw new SmtpOAuth2Exception("Microsoft OAuth2 no devolvió access_token para SMTP.");

            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new SmtpOAuth2Exception("Microsoft OAuth2 devolvió un access_token SMTP vacío.");

            return accessToken;
        }
        catch (JsonException ex)
        {
            throw new SmtpOAuth2Exception("Microsoft OAuth2 devolvió una respuesta inválida para SMTP.", ex);
        }
    }

    private static SecureSocketOptions ResolverSeguridad(ConfiguracionSmtp configuracion)
    {
        if (!configuracion.UsarSsl)
            return SecureSocketOptions.None;

        return configuracion.Puerto == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static string ObtenerNombreSeguridad(ConfiguracionSmtp configuracion) =>
        ResolverSeguridad(configuracion) switch
        {
            SecureSocketOptions.SslOnConnect => "SSL/TLS directo",
            SecureSocketOptions.StartTls => "STARTTLS obligatorio",
            _ => "Sin TLS"
        };

    private static async Task EjecutarConTimeoutAsync(
        Func<CancellationToken, Task> accion,
        int timeoutSegundos,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSegundos));

        try
        {
            await accion(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("El servidor SMTP no respondió dentro del tiempo configurado.");
        }
    }

    private ResultadoEntregaEmail CrearFalloTecnico(
        Exception ex,
        string destinatario,
        ConfiguracionSmtp configuracion,
        int intentos,
        string messageId)
    {
        var (codigo, mensaje, transitorio) = MapearError(ex, configuracion);

        _logger.LogError(
            ex,
            "Fallo SMTP definitivo. Codigo={Codigo}; Intentos={Intentos}; Destinatario={DestinatarioEnmascarado}; Host={Host}; Puerto={Puerto}; Seguridad={Seguridad}; MessageId={MessageId}.",
            codigo,
            intentos,
            EnmascararCorreo(destinatario),
            configuracion.Host,
            configuracion.Puerto,
            ObtenerNombreSeguridad(configuracion),
            messageId);

        return Fallo(codigo, mensaje, transitorio, intentos, messageId);
    }

    private static (string Codigo, string Mensaje, bool Transitorio) MapearError(
        Exception ex,
        ConfiguracionSmtp configuracion)
    {
        if (ex is TimeoutException)
            return ("SMTP_TIMEOUT", "El servidor de correo no respondió dentro del tiempo permitido.", true);

        if (ex is SmtpOAuth2Exception)
            return ("SMTP_OAUTH2", ex.Message, false);

        if (ex is MailKit.Security.AuthenticationException or ServiceNotAuthenticatedException)
            return ("SMTP_AUTENTICACION", "Microsoft Outlook rechazó la autenticación OAuth2 del entorno actual.", false);

        if (ex is SslHandshakeException or NotSupportedException)
            return ("SMTP_TLS", "No se pudo establecer STARTTLS con el servidor SMTP configurado.", false);

        if (ex is SmtpCommandException smtp)
        {
            var status = (int)smtp.StatusCode;
            if (status is 534 or 535)
                return ("SMTP_AUTENTICACION", "El servidor SMTP rechazó la autenticación OAuth2.", false);

            if (status is >= 400 and <= 499)
                return ("SMTP_TEMPORAL", "El servidor de correo presentó un problema temporal. Intenta nuevamente más tarde.", true);

            if (status is 550 or 551 or 552 or 553 or 554)
                return ("SMTP_DESTINATARIO_RECHAZADO", "El servidor rechazó el remitente, el destinatario o el contenido del mensaje. Revisa los logs del entorno actual.", false);

            return ("SMTP_RECHAZADO", "El servidor SMTP rechazó la operación. Revisa el diagnóstico y los logs del entorno actual.", false);
        }

        if (ex is SocketException or IOException or SmtpProtocolException or ServiceNotConnectedException)
            return ("SMTP_CONEXION", "No fue posible establecer una conexión estable con el servidor SMTP.", true);

        return ("SMTP_ERROR", "No se pudo enviar el correo. Revisa el diagnóstico SMTP y los logs del backend del entorno actual.", false);
    }

    private static bool EsErrorTransitorio(Exception ex)
    {
        if (ex is TimeoutException or IOException or SocketException or SmtpProtocolException or ServiceNotConnectedException)
            return true;

        if (ex.InnerException is IOException or SocketException or TimeoutException)
            return true;

        return ex is SmtpCommandException smtp && (int)smtp.StatusCode is >= 400 and <= 499;
    }

    private static TimeSpan CalcularDemora(int baseMilliseconds, int intento)
    {
        var factor = Math.Pow(2, Math.Max(0, intento - 1));
        return TimeSpan.FromMilliseconds(Math.Min(baseMilliseconds * factor, 10_000));
    }

    private static ResultadoEntregaEmail Fallo(
        string codigo,
        string error,
        bool esTransitorio,
        int intentos,
        string? messageId = null) => new()
        {
            Exito = false,
            Codigo = codigo,
            Error = error,
            EsTransitorio = esTransitorio,
            Intentos = intentos,
            MessageId = messageId
        };

    private static ResultadoDiagnosticoSmtp CrearDiagnostico(
        bool exito,
        string codigo,
        string mensaje,
        ConfiguracionSmtp configuracion,
        bool autenticado,
        long duracionMilisegundos) => new()
        {
            Exito = exito,
            Codigo = codigo,
            Mensaje = mensaje,
            Host = EnmascararHost(configuracion.Host),
            Puerto = configuracion.Puerto,
            ModoSeguridad = ObtenerNombreSeguridad(configuracion),
            Autenticado = autenticado,
            DuracionMilisegundos = (int)Math.Clamp(duracionMilisegundos, 0, int.MaxValue)
        };

    private static bool EsPlaceholder(string valor) =>
        valor.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
        valor.Equals("REPLACE_ME", StringComparison.OrdinalIgnoreCase) ||
        valor.Contains("not-used", StringComparison.OrdinalIgnoreCase);

    private static bool EsOutlook(string host) =>
        host.Equals("smtp-mail.outlook.com", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("smtp.office365.com", StringComparison.OrdinalIgnoreCase);

    private static string LimpiarEncabezado(string? valor) =>
        (valor ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();

    private static string LimpiarNombreArchivo(string? nombre)
    {
        var limpio = Path.GetFileName(nombre ?? "adjunto.bin");
        return string.IsNullOrWhiteSpace(limpio) ? "adjunto.bin" : limpio;
    }

    private static string ConvertirHtmlATexto(string html)
    {
        var texto = Regex.Replace(html, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        texto = Regex.Replace(texto, "</p>", "\n\n", RegexOptions.IgnoreCase);
        texto = Regex.Replace(texto, "<[^>]+>", string.Empty);
        texto = WebUtility.HtmlDecode(texto);
        return Regex.Replace(texto, "[ \\t]+", " ").Trim();
    }

    private static string EnmascararCorreo(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return "***";
        var partes = correo.Split('@', 2);
        if (partes.Length != 2 || partes[0].Length == 0) return "***";
        var usuario = partes[0].Length <= 2 ? partes[0][0] + "***" : partes[0][..2] + "***";
        return $"{usuario}@{partes[1]}";
    }

    private static string EnmascararHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) || EsPlaceholder(host)) return "No configurado";
        var partes = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length <= 2 ? host : $"***.{string.Join('.', partes[^2..])}";
    }

    private sealed record ConfiguracionSmtp(
        string Host,
        int Puerto,
        string Usuario,
        string OAuth2ClientId,
        string OAuth2RefreshToken,
        bool UsarSsl,
        bool RequiereAutenticacion,
        string CorreoRemitente,
        string NombreRemitente,
        string? CorreoRespuesta,
        int TimeoutSegundos,
        int MaximoIntentos,
        int RetryBaseDelayMilliseconds);

    private sealed class SmtpOAuth2Exception : Exception
    {
        public SmtpOAuth2Exception(string message) : base(message) { }
        public SmtpOAuth2Exception(string message, Exception innerException) : base(message, innerException) { }
    }
}
