using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface ICargaMasivaService
{
    CargaMasivaConfiguracionDto ObtenerConfiguracion();
    Task<ArchivoDescargableDto> DescargarPlantillaAsync(TipoCargaMasiva tipo, string formato);
    Task<CargaMasivaDetalleDto> ValidarAsync(
        TipoCargaMasiva tipo,
        string nombreArchivo,
        string? contentType,
        long tamanoBytes,
        Stream contenido,
        CancellationToken cancellationToken = default);
    Task<PagedResult<CargaMasivaDto>> GetPagedAsync(PagedRequest request);
    Task<CargaMasivaDetalleDto?> GetByIdAsync(int id);
    Task<CargaMasivaDetalleDto> ConfirmarAsync(int id, CancellationToken cancellationToken = default);
    Task<ArchivoDescargableDto> DescargarErroresAsync(int id, string formato);
}
