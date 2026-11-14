using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IThreeWayMatchService
{
    Task<ThreeWayMatchResultDto> EvaluarAsync(int ordenCompraId, CancellationToken cancellationToken = default);
}
