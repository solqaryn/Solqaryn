namespace Solqaryn.Application.DTOs;

public class EnlaceCompartirDto
{
    public string UrlPdfPublica { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
    public string MensajeWhatsApp { get; set; } = string.Empty;
    public string TelefonoSugerido { get; set; } = string.Empty;
}

public class RegistrarEnvioDto
{
    public string Canal { get; set; } = string.Empty; // "WhatsApp" | "Correo"
    public string Destinatario { get; set; } = string.Empty;
    public string Resultado { get; set; } = "Iniciado";
    public string? Error { get; set; }
}

public class HistorialEnvioDto
{
    public int Id { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string? Error { get; set; }
    public string? UsuarioNombre { get; set; }
    public DateTime Fecha { get; set; }
}

public class EnviarCorreoFacturaDto
{
    public string Destinatario { get; set; } = string.Empty;
}
