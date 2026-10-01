using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IEmpresaService
{
    Task<List<EmpresaDto>> ListAsync(bool? activa = null, CancellationToken cancellationToken = default);
    Task<EmpresaDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmpresaDto> CreateAsync(CreateEmpresaDto dto, CancellationToken cancellationToken = default);
    Task<EmpresaDto?> UpdateAsync(int id, UpdateEmpresaDto dto, CancellationToken cancellationToken = default);
    Task<EmpresaDto?> CambiarEstadoAsync(int id, bool activa, CancellationToken cancellationToken = default);
}
