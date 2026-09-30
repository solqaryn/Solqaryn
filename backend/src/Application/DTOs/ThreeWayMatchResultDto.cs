using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public record ThreeWayMatchResultDto(
    int OrdenCompraId,
    ThreeWayMatchStatus Estado,
    IReadOnlyCollection<ThreeWayMatchLineDiscrepancyDto> Discrepancias);

public record ThreeWayMatchLineDiscrepancyDto(
    int OrdenCompraDetalleId,
    ThreeWayMatchDiscrepancyType Tipo,
    decimal EsperadoOrdenado,
    decimal ValorRecepcion,
    decimal ValorFacturado,
    string Mensaje,
    string? EsperadoTexto = null,
    string? ValorFacturadoTexto = null);
