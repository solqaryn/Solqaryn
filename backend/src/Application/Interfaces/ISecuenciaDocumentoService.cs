using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ISecuenciaDocumentoService
{
    Task<SecuenciaDocumentoConsultaDto> ObtenerAsync(
        int empresaId,
        int? sucursalId,
        string tipoDocumento,
        CancellationToken cancellationToken = default);

    Task<SecuenciaDocumentoSiguienteDto> ReservarSiguienteAsync(
        ReservarSecuenciaDocumentoRequest request,
        CancellationToken cancellationToken = default);
}
