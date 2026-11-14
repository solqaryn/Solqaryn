using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public sealed record RegistrarMensajeOutboxRequest(
    string TipoEvento,
    string PayloadJson,
    string? TipoAgregado = null,
    string? IdAgregado = null,
    string? CorrelationId = null);

public interface IMensajeOutboxService
{
    Task<MensajeOutbox> RegistrarAsync(
        int empresaId,
        RegistrarMensajeOutboxRequest request,
        string claveIdempotencia,
        CancellationToken cancellationToken = default);
}
