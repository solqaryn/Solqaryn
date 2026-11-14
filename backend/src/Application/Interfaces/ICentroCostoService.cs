using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.Interfaces;

public interface ICentroCostoService
{
    Task<(List<CentroCostoDto> Items, int Total)> BuscarAsync(string? termino, TipoCentroCosto? tipo, int? sucursalId, bool? activo, int pagina, int tamanoPagina);
    Task<List<CentroCostoDto>> GetActivosAsync(TipoCentroCosto? tipo = null, int? sucursalId = null);
    Task<CentroCostoDto?> GetByIdAsync(int id);
    Task<CentroCostoDto> CreateAsync(CreateCentroCostoDto dto);
    Task<CentroCostoDto?> UpdateAsync(int id, UpdateCentroCostoDto dto);
    Task<CentroCostoDto?> CambiarEstadoAsync(int id, bool activo);
    Task<bool> DeleteAsync(int id);
}
