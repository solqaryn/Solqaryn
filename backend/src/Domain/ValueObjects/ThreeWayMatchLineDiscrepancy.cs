using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.ValueObjects;

public sealed record ThreeWayMatchLineDiscrepancy(
    int OrdenCompraDetalleId,
    ThreeWayMatchDiscrepancyType Tipo,
    decimal EsperadoOrdenado,
    decimal ValorRecepcion,
    decimal ValorFacturado,
    string Mensaje,
    string? EsperadoTexto = null,
    string? ValorFacturadoTexto = null);
