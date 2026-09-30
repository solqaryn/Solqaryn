using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IFacturaService
{
    Task<FacturaDto?> GetByIdAsync(int id);
    Task<FacturaDto?> GetByVentaIdAsync(int ventaId);
    Task<List<FacturaDto>> GetAllAsync();
    Task<FacturaDto> RegistrarPagoAsync(int facturaId, RegistrarFacturaPagoDto dto, int? usuarioId, string? nombreUsuario);
    Task<FacturaDto> AnularPagoAsync(int facturaId, int pagoId, AnularFacturaPagoDto dto, int? usuarioId, string? nombreUsuario);
    Task<FacturaDto> CambiarEstadoAsync(int facturaId, CambiarEstadoFacturaDto dto, int? usuarioId, string? nombreUsuario);

    /// <summary>
    /// Uso exclusivo del flujo de enlace público después de validar el token.
    /// No debe exponerse directamente desde un controlador.
    /// </summary>
    Task<FacturaDto?> GetByIdParaEnlacePublicoValidadoAsync(int id);
}
