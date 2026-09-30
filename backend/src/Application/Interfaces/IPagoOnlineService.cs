using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IPagoOnlineService
{
    Task<InicioPagoOnlineResultadoDto> IniciarAsync(
        IniciarPagoOnlineDto solicitud,
        string claveIdempotencia,
        CancellationToken cancellationToken = default);

    Task<PagoOnlineDto?> ObtenerAsync(
        int empresaId,
        int id,
        CancellationToken cancellationToken = default);

    Task<PaginaPagosOnlineDto> ListarAsync(
        int empresaId,
        int pagina,
        int tamanoPagina,
        EstadoPagoOnline? estado,
        int? facturaId,
        string? proveedor,
        CancellationToken cancellationToken = default);
}
