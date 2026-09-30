namespace Solqaryn.Domain.Entities;

/// Registro de cada intento de compartir una factura (sección 14/15/18:
/// "registrar el intento", "registrar el resultado"). Un intento por
/// WhatsApp se registra cuando el usuario abre el enlace de wa.me (no hay
/// forma de saber si el destinatario lo recibió realmente sin una API
/// oficial de WhatsApp — se documenta esta limitación real, no se simula
/// una confirmación de entrega que no existe).
public class HistorialEnvioFactura
{
    public int Id { get; set; }
    public int FacturaId { get; set; }

    public string Canal { get; set; } = string.Empty; // "WhatsApp" | "Correo"
    public string Destinatario { get; set; } = string.Empty; // número o correo usado
    public string Resultado { get; set; } = string.Empty; // "Iniciado" | "Enviado" | "Error"
    public string? Error { get; set; }

    public int? UsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
