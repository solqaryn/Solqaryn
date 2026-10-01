using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IPagoOnlineRepository
{
    Task<Factura?> ObtenerFacturaAsync(int facturaId, CancellationToken cancellationToken = default);

    Task<PagoOnline?> ObtenerPorIdAsync(
        int empresaId,
        int id,
        CancellationToken cancellationToken = default);

    Task<PagoOnline?> ObtenerPorIdempotenciaAsync(
        int empresaId,
        string proveedor,
        string claveIdempotenciaHash,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PagoOnline> Items, int Total)> ListarAsync(
        int empresaId,
        int pagina,
        int tamanoPagina,
        EstadoPagoOnline? estado,
        int? facturaId,
        string? proveedor,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(PagoOnline pago, CancellationToken cancellationToken = default);
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
