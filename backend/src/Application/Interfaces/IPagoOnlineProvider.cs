using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Puerto provider-agnostic para iniciar pagos online. Las credenciales permanecen
/// en infraestructura/configuración server-side y nunca forman parte del contrato.
/// </summary>
public interface IPagoOnlineProvider
{
    string Codigo { get; }

    Task<ResultadoInicioPagoOnline> IniciarAsync(
        SolicitudInicioPagoOnline solicitud,
        CancellationToken cancellationToken = default);
}

public sealed record SolicitudInicioPagoOnline(
    int EmpresaId,
    int FacturaId,
    decimal Monto,
    string Moneda,
    string ReferenciaFactura,
    string ClaveIdempotencia = "");

public sealed record ResultadoInicioPagoOnline(
    string ReferenciaProveedor,
    Uri UrlPago,
    EstadoPagoOnline Estado);
