using System.Net;
using System.Net.Sockets;
using System.Text;
using InventoryApp.Application.Interfaces;
using InventoryApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventoryApp.Tests;

public class SmtpEmailServiceTests
{
    [Fact]
    public void ObtenerEstadoConfiguracion_No_Expone_Secretos_Y_Declara_Seguridad()
    {
        var service = CrearServicio(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp-mail.outlook.com",
            ["Smtp:Port"] = "587",
            ["Smtp:UsuarioSmtp"] = "solqaryn.platform@outlook.com",
            ["Smtp:OAuth2ClientId"] = "00000000-0000-0000-0000-000000000001",
            ["Smtp:OAuth2RefreshToken"] = "refresh-token-super-sensible",
            ["Smtp:UsarSsl"] = "true",
            ["Smtp:RequiereAutenticacion"] = "true",
            ["Smtp:MaxAttempts"] = "3"
        });

        var estado = service.ObtenerEstadoConfiguracion();

        Assert.True(estado.Configurado);
        Assert.Equal("***.outlook.com", estado.Host);
        Assert.Equal("so***@outlook.com", estado.RemitenteEnmascarado);
        Assert.DoesNotContain("refresh-token", estado.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("STARTTLS obligatorio", estado.ModoSeguridad);
        Assert.Equal(3, estado.MaximoIntentos);
    }

    [Fact]
    public void ObtenerEstadoConfiguracion_OAuth2_Configura_Outlook_Sin_Credencial_Legacy()
    {
        var service = CrearServicio(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp-mail.outlook.com",
            ["Smtp:Port"] = "587",
            ["Smtp:UsuarioSmtp"] = "solqaryn.platform@outlook.com",
            ["Smtp:OAuth2ClientId"] = "00000000-0000-0000-0000-000000000001",
            ["Smtp:OAuth2RefreshToken"] = "refresh-token-de-prueba",
            ["Smtp:UsarSsl"] = "true",
            ["Smtp:RequiereAutenticacion"] = "true",
            ["Smtp:CorreoRemitente"] = "solqaryn.platform@outlook.com"
        });

        var estado = service.ObtenerEstadoConfiguracion();

        Assert.True(estado.Configurado, estado.Mensaje);
        Assert.Equal("***.outlook.com", estado.Host);
        Assert.Equal("so***@outlook.com", estado.RemitenteEnmascarado);
        Assert.Equal("STARTTLS obligatorio", estado.ModoSeguridad);
    }

    [Fact]
    public void ObtenerEstadoConfiguracion_OAuth2_Requiere_RefreshToken()
    {
        var service = CrearServicio(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "smtp-mail.outlook.com",
            ["Smtp:Port"] = "587",
            ["Smtp:UsuarioSmtp"] = "solqaryn.platform@outlook.com",
            ["Smtp:OAuth2ClientId"] = "00000000-0000-0000-0000-000000000001",
            ["Smtp:UsarSsl"] = "true",
            ["Smtp:RequiereAutenticacion"] = "true",
            ["Smtp:CorreoRemitente"] = "solqaryn.platform@outlook.com"
        });

        var estado = service.ObtenerEstadoConfiguracion();

        Assert.False(estado.Configurado);
        Assert.Contains("refresh token", estado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProbarConexionAsync_Comprueba_Conexion_Y_Autenticacion_Sin_Enviar_Mensaje()
    {
        await using var servidor = new FakeSmtpServer(fallosTransitorios: 0);
        await servidor.StartAsync();
        var service = CrearServicio(ConfiguracionServidor(servidor.Port));

        var resultado = await service.ProbarConexionAsync();

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal("SMTP_OK", resultado.Codigo);
        Assert.False(resultado.Autenticado);
        Assert.Equal("Sin TLS", resultado.ModoSeguridad);
        Assert.Equal(0, servidor.IntentosData);
        Assert.Empty(servidor.Mensajes);
    }

    [Fact]
    public async Task EnviarAsync_Reintenta_Error_Transitorio_Y_Adjunta_Pdf()
    {
        await using var servidor = new FakeSmtpServer(fallosTransitorios: 1);
        await servidor.StartAsync();

        var service = CrearServicio(ConfiguracionServidor(servidor.Port));
        var pdf = Encoding.UTF8.GetBytes("%PDF-1.7 factura fase 7");
        var resultado = await service.EnviarAsync(
            "cliente@example.com",
            "Factura FAC-000001",
            "<p>Factura de prueba</p>",
            new List<AdjuntoCorreo>
            {
                new()
                {
                    NombreArchivo = "FAC-000001.pdf",
                    ContentType = "application/pdf",
                    Contenido = pdf
                }
            });

        Assert.True(resultado.Exito, resultado.Error);
        Assert.Equal(2, resultado.Intentos);
        Assert.Equal("ENVIADO", resultado.Codigo);
        Assert.False(string.IsNullOrWhiteSpace(resultado.MessageId));
        Assert.Equal(2, servidor.IntentosData);
        Assert.Single(servidor.Mensajes);

        var mensaje = servidor.Mensajes.Single();
        Assert.Contains("cliente@example.com", mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FAC-000001.pdf", mensaje, StringComparison.Ordinal);
        Assert.Contains("application/pdf", mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Convert.ToBase64String(pdf), mensaje.Replace("\r", string.Empty).Replace("\n", string.Empty), StringComparison.Ordinal);
        Assert.Contains("text/plain", mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text/html", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnviarAsync_Rechaza_Destinatario_Mal_Formado_Sin_Conectar()
    {
        var service = CrearServicio(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "127.0.0.1",
            ["Smtp:Port"] = "1025",
            ["Smtp:UsuarioSmtp"] = "facturas@desarrollo.test",
            ["Smtp:UsarSsl"] = "false",
            ["Smtp:RequiereAutenticacion"] = "false"
        });

        var resultado = await service.EnviarAsync("correo invalido", "Factura", "<p>Prueba</p>");

        Assert.False(resultado.Exito);
        Assert.Equal("DESTINATARIO_INVALIDO", resultado.Codigo);
        Assert.Equal(0, resultado.Intentos);
    }

    private static Dictionary<string, string?> ConfiguracionServidor(int puerto) => new()
    {
        ["Smtp:Host"] = "127.0.0.1",
        ["Smtp:Port"] = puerto.ToString(),
        ["Smtp:UsuarioSmtp"] = "facturas@desarrollo.test",
        ["Smtp:UsarSsl"] = "false",
        ["Smtp:RequiereAutenticacion"] = "false",
        ["Smtp:NombreRemitente"] = "SOLQARYN DEV",
        ["Smtp:TimeoutSeconds"] = "10",
        ["Smtp:MaxAttempts"] = "3",
        ["Smtp:RetryBaseDelayMilliseconds"] = "50"
    };

    private static SmtpEmailService CrearServicio(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new SmtpEmailService(configuration, NullLogger<SmtpEmailService>.Instance);
    }

    private sealed class FakeSmtpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();
        private readonly int _fallosTransitoriosIniciales;
        private Task? _loopTask;
        private int _fallosRestantes;
        private int _intentosData;
        private readonly List<string> _mensajes = new();
        private readonly object _sync = new();

        public FakeSmtpServer(int fallosTransitorios)
        {
            _fallosTransitoriosIniciales = fallosTransitorios;
            _fallosRestantes = fallosTransitorios;
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public int IntentosData => Volatile.Read(ref _intentosData);
        public IReadOnlyList<string> Mensajes
        {
            get { lock (_sync) return _mensajes.ToList(); }
        }

        public Task StartAsync()
        {
            _listener.Start();
            _loopTask = Task.Run(AcceptLoopAsync);
            return Task.CompletedTask;
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            await using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, leaveOpen: true))
            await using (var writer = new StreamWriter(stream, Encoding.ASCII, 4096, leaveOpen: true)
            {
                NewLine = "\r\n",
                AutoFlush = true
            })
            {
                await writer.WriteLineAsync("220 fake-smtp ESMTP ready");
                var autenticado = false;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line is null) break;

                    if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250-fake-smtp");
                        await writer.WriteLineAsync("250-AUTH LOGIN");
                        await writer.WriteLineAsync("250 SIZE 20971520");
                    }
                    else if (line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250 fake-smtp");
                    }
                    else if (line.StartsWith("AUTH LOGIN", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        string username;
                        if (parts.Length >= 3)
                        {
                            username = Decodificar(parts[2]);
                        }
                        else
                        {
                            await writer.WriteLineAsync("334 VXNlcm5hbWU6");
                            username = Decodificar(await reader.ReadLineAsync(cancellationToken) ?? string.Empty);
                        }

                        await writer.WriteLineAsync("334 UGFzc3dvcmQ6");
                        var password = Decodificar(await reader.ReadLineAsync(cancellationToken) ?? string.Empty);
                        autenticado = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
                        await writer.WriteLineAsync(autenticado ? "235 2.7.0 Authentication successful" : "535 5.7.8 Authentication failed");
                    }
                    else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase) ||
                             line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250 2.1.0 OK");
                    }
                    else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                    {
                        Interlocked.Increment(ref _intentosData);
                        await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                        var builder = new StringBuilder();
                        while (true)
                        {
                            var dataLine = await reader.ReadLineAsync(cancellationToken);
                            if (dataLine is null || dataLine == ".") break;
                            if (dataLine.StartsWith("..", StringComparison.Ordinal)) dataLine = dataLine[1..];
                            builder.Append(dataLine).Append("\r\n");
                        }

                        if (Interlocked.CompareExchange(ref _fallosRestantes, 0, 0) > 0)
                        {
                            Interlocked.Decrement(ref _fallosRestantes);
                            await writer.WriteLineAsync("451 4.3.0 Temporary local problem");
                        }
                        else
                        {
                            lock (_sync) _mensajes.Add(builder.ToString());
                            await writer.WriteLineAsync("250 2.0.0 Queued");
                        }
                    }
                    else if (line.Equals("RSET", StringComparison.OrdinalIgnoreCase) ||
                             line.Equals("NOOP", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250 OK");
                    }
                    else if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("221 2.0.0 Bye");
                        break;
                    }
                    else
                    {
                        await writer.WriteLineAsync("250 OK");
                    }
                }
            }
        }

        private static string Decodificar(string value)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
            catch { return string.Empty; }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();
            if (_loopTask is not null)
            {
                try { await _loopTask; }
                catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            Assert.True(_fallosRestantes <= _fallosTransitoriosIniciales);
        }
    }
}
