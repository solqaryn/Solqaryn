using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ITransferenciaInventarioMovimientoService
{
    Task<TransferenciaInventarioDto?> DespacharAsync(int id, DespacharTransferenciaInventarioDto dto);
    Task<TransferenciaInventarioDto?> RecibirAsync(int id, RecibirTransferenciaInventarioDto dto);
    Task<TransferenciaInventarioDto?> CancelarAsync(int id, CancelarTransferenciaInventarioDto dto);
}
