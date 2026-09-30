using Solqaryn.Application.DTOs.Contabilidad;

namespace Solqaryn.Application.Interfaces;

public sealed record AsientoContableWriteResult(
    AsientoContableDto Asiento,
    bool Created,
    int Id);

public interface IAsientoContableWriter
{
    Task<AsientoContableWriteResult> CreateAsync(
        CrearAsientoContableDto dto,
        CancellationToken cancellationToken = default);
}
