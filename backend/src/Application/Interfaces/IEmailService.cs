namespace InventoryApp.Application.Interfaces;

public class AdjuntoCorreo
{
    public string NombreArchivo { get; set; } = string.Empty;
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

public sealed class ResultadoEntregaEmail
{
    public bool Exito { get; init; }
    public string? Error { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public bool EsTransitorio { get; init; }
    public int Intentos { get; init; }
    public string? MessageId { get; init; }
}

public sealed class EstadoConfiguracionSmtp
{
    public bool Configurado { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Puerto { get; init; }
    public bool UsaTls { get; init; }
    public string ModoSeguridad { get; init; } = string.Empty;
    public bool RequiereAutenticacion { get; init; }
    public string RemitenteEnmascarado { get; init; } = string.Empty;
    public int MaximoIntentos { get; init; }
    public int TimeoutSegundos { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}

public sealed class ResultadoDiagnosticoSmtp
{
    public bool Exito { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Puerto { get; init; }
    public string ModoSeguridad { get; init; } = string.Empty;
    public bool Autenticado { get; init; }
    public int DuracionMilisegundos { get; init; }
}

/// Envío transaccional de correo vía SMTP configurable. Las credenciales se
/// obtienen exclusivamente de configuración o variables de entorno.
public interface IEmailService
{
    Task<ResultadoEntregaEmail> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        List<AdjuntoCorreo>? adjuntos = null,
        CancellationToken cancellationToken = default);

    EstadoConfiguracionSmtp ObtenerEstadoConfiguracion();

    /// Comprueba conexión, negociación TLS y autenticación sin enviar correo ni
    /// exponer secretos. Se usa para validar el transporte SMTP del entorno actual.
    Task<ResultadoDiagnosticoSmtp> ProbarConexionAsync(CancellationToken cancellationToken = default);
}
