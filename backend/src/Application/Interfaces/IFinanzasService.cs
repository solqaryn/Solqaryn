using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IFinanzasService
{
    Task<FinanzasResumenDto> GetResumenAsync();
    Task<List<MovimientoFinancieroDto>> GetMovimientosAsync(DateTime? desde, DateTime? hasta);
    Task<MovimientoFinancieroDto> RegistrarMovimientoManualAsync(CreateMovimientoManualDto dto);
    Task<MovimientoFinancieroDto?> AnularMovimientoAsync(int id, string motivo);
    Task<List<RevisionFinancieraDto>> GetRevisionesAsync();
    Task<RevisionFinancieraDto> RegistrarRevisionAsync(CreateRevisionFinancieraDto dto);
}
