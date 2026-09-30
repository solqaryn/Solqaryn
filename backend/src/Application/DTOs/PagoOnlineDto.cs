using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed record IniciarPagoOnlineDto(
    int EmpresaId,
    int FacturaId,
    string Proveedor,
    decimal Monto,
    string Moneda);

public sealed record PagoOnlineDto(
    int Id,
    int EmpresaId,
    int FacturaId,
    string Proveedor,
    string? ReferenciaProveedor,
    decimal Monto,
    string Moneda,
    EstadoPagoOnline Estado,
    string? UrlPago,
    DateTime CreadoUtc,
    DateTime? ConfirmadoUtc,
    string? UltimoError);

public sealed record InicioPagoOnlineResultadoDto(
    PagoOnlineDto Pago,
    bool Reutilizado);

public sealed record PaginaPagosOnlineDto(
    IReadOnlyList<PagoOnlineDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina);
