using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class ThreeWayMatchService : IThreeWayMatchService
{
    private const int PageSize = 100;

    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly IRecepcionCompraRepository _recepcionCompraRepository;
    private readonly IFacturaProveedorRepository _facturaProveedorRepository;

    public ThreeWayMatchService(
        IOrdenCompraRepository ordenCompraRepository,
        IRecepcionCompraRepository recepcionCompraRepository,
        IFacturaProveedorRepository facturaProveedorRepository)
    {
        _ordenCompraRepository = ordenCompraRepository ?? throw new ArgumentNullException(nameof(ordenCompraRepository));
        _recepcionCompraRepository = recepcionCompraRepository ?? throw new ArgumentNullException(nameof(recepcionCompraRepository));
        _facturaProveedorRepository = facturaProveedorRepository ?? throw new ArgumentNullException(nameof(facturaProveedorRepository));
    }

    public async Task<ThreeWayMatchResultDto> EvaluarAsync(
        int ordenCompraId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ordenCompra = await _ordenCompraRepository.GetByIdAsync(ordenCompraId)
            ?? throw new ResourceNotFoundException($"No existe la orden de compra {ordenCompraId}.");

        var recepciones = await ObtenerRecepcionesVigentesAsync(ordenCompraId, cancellationToken);
        var facturas = await ObtenerFacturasVigentesAsync(ordenCompraId, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        return Mapear(ThreeWayMatchResult.Evaluar(ordenCompra, recepciones, facturas));
    }

    private async Task<IReadOnlyList<RecepcionCompra>> ObtenerRecepcionesVigentesAsync(
        int ordenCompraId,
        CancellationToken cancellationToken)
    {
        var resultado = new Dictionary<int, RecepcionCompra>();
        int? totalEsperado = null;

        for (var page = 1; ; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (items, total) = await _recepcionCompraRepository.GetPagedAsync(new RecepcionCompraQueryDto
            {
                OrdenCompraId = ordenCompraId,
                Estado = EstadoRecepcionCompra.Recibida,
                Page = page,
                PageSize = PageSize
            });

            ValidarTotalEstable(totalEsperado, total, "recepciones");
            totalEsperado ??= total;

            foreach (var item in items)
                resultado[item.Id] = item;

            if (resultado.Count == total)
                return resultado.Values.ToList();

            if (items.Count == 0 || resultado.Count > total)
                throw EvidenciaInestable("recepciones");
        }
    }

    private async Task<IReadOnlyList<FacturaProveedor>> ObtenerFacturasVigentesAsync(
        int ordenCompraId,
        CancellationToken cancellationToken)
    {
        var resultado = new Dictionary<int, FacturaProveedor>();
        int? totalEsperado = null;

        for (var page = 1; ; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (items, total) = await _facturaProveedorRepository.GetPagedAsync(new FacturaProveedorFiltroDto
            {
                OrdenCompraId = ordenCompraId,
                Estado = EstadoFacturaProveedor.Registrada,
                SortBy = "numero",
                SortDirection = "asc",
                Page = page,
                PageSize = PageSize
            });

            ValidarTotalEstable(totalEsperado, total, "facturas de proveedor");
            totalEsperado ??= total;

            foreach (var item in items)
                resultado[item.Id] = item;

            if (resultado.Count == total)
                return resultado.Values.ToList();

            if (items.Count == 0 || resultado.Count > total)
                throw EvidenciaInestable("facturas de proveedor");
        }
    }

    private static void ValidarTotalEstable(int? esperado, int actual, string evidencia)
    {
        if (esperado.HasValue && esperado.Value != actual)
            throw EvidenciaInestable(evidencia);
    }

    private static BusinessRuleException EvidenciaInestable(string evidencia) =>
        new($"La evidencia de {evidencia} cambió durante la conciliación. Reintenta la evaluación para obtener un conjunto consistente.");

    private static ThreeWayMatchResultDto Mapear(ThreeWayMatchResult resultado) =>
        new(
            resultado.OrdenCompraId,
            resultado.Estado,
            resultado.Discrepancias
                .Select(x => new ThreeWayMatchLineDiscrepancyDto(
                    x.OrdenCompraDetalleId,
                    x.Tipo,
                    x.EsperadoOrdenado,
                    x.ValorRecepcion,
                    x.ValorFacturado,
                    x.Mensaje,
                    x.EsperadoTexto,
                    x.ValorFacturadoTexto))
                .ToArray());
}
