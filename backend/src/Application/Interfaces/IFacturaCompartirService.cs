using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IFacturaCompartirService
{
    /// Genera un enlace público temporal nuevo. Por seguridad, los enlaces
    /// anteriores de la misma factura quedan expirados automáticamente.
    Task<EnlaceCompartirDto> PrepararCompartirAsync(int facturaId);

    Task RegistrarIntentoAsync(int facturaId, RegistrarEnvioDto dto);

    Task<List<HistorialEnvioDto>> GetHistorialAsync(int facturaId);

    /// Revoca todos los enlaces públicos todavía vigentes de la factura.
    Task<int> RevocarEnlacesAsync(int facturaId);

    /// Sirve el mismo PDF oficial mediante un token válido, no expirado y con
    /// límite de accesos. El token se recibe en claro, pero solo su hash se
    /// compara contra la base de datos.
    Task<(byte[] Pdf, string NombreArchivo)?> ObtenerPdfPorTokenAsync(string token);

    /// Envía el PDF oficial A4 como adjunto. Una clave de idempotencia evita
    /// duplicados causados por doble clic o reintentos del transporte HTTP.
    Task<ResultadoEnvioCorreoDto> EnviarPorCorreoAsync(
        int facturaId,
        string destinatario,
        string? claveIdempotencia = null,
        CancellationToken cancellationToken = default);
}
