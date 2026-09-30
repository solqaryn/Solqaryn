using Solqaryn.Application.DTOs.Contabilidad;

namespace Solqaryn.Application.Interfaces;

public interface IContabilizacionService
{
    Task<AsientoContableWriteResult> ContabilizarAsync(
        EventoContableDto evento,
        CancellationToken cancellationToken = default);
}
