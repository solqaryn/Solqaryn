using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public sealed class KardexMovimientoWriter : IKardexMovimientoWriter
{
    private readonly IMovimientoInventarioRepository _repository;

    public KardexMovimientoWriter(IMovimientoInventarioRepository repository)
    {
        _repository = repository;
    }

    public Task RegistrarCorrelacionadoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        string correlationId)
    {
        if (origen.TransferenciaInventarioId.HasValue)
        {
            PrepararTransferencia(movimiento, origen, correlationId, contexto: null);
            return _repository.AddAsync(movimiento);
        }

        return _repository.AddConOrigenTipadoCorrelacionadoAsync(movimiento, origen, correlationId);
    }

    public Task RegistrarFisicoAsync(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        ContextoFisicoMovimientoInventario contexto)
    {
        if (origen.TransferenciaInventarioId.HasValue)
        {
            PrepararTransferencia(movimiento, origen, contexto.CorrelationId, contexto);
            return _repository.AddAsync(movimiento);
        }

        return _repository.AddConOrigenTipadoAsync(movimiento, origen, contexto);
    }

    private static void PrepararTransferencia(
        MovimientoInventario movimiento,
        OrigenMovimientoInventario origen,
        string correlationId,
        ContextoFisicoMovimientoInventario? contexto)
    {
        ArgumentNullException.ThrowIfNull(movimiento);
        ArgumentNullException.ThrowIfNull(origen);
        var transferenciaId = origen.TransferenciaInventarioId
            ?? throw new InvalidOperationException("El origen no corresponde a una transferencia de inventario.");
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new InvalidOperationException("CorrelationId es obligatorio para Kardex de transferencias.");

        movimiento.ReferenciaTipo = "TransferenciaInventario";
        movimiento.ReferenciaId = transferenciaId;
        movimiento.CompraId = null;
        movimiento.VentaId = null;
        movimiento.ConsumoInsumoId = null;
        movimiento.AjusteInventarioId = null;
        movimiento.TransferenciaInventarioId = transferenciaId;
        movimiento.CorrelationId = correlationId.Trim();

        if (contexto is null)
            return;

        movimiento.ProductoVarianteId = contexto.ProductoVarianteId;
        movimiento.AlmacenId = contexto.AlmacenId;
        movimiento.UbicacionAlmacenId = contexto.UbicacionAlmacenId;
        movimiento.CorrelationId = contexto.CorrelationId;
    }
}
