using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

/// <summary>
/// Encapsula la escritura canónica de Kardex para el ciclo de vida de Venta.
/// Centraliza los CorrelationId determinísticos para que Confirmar/Anular no
/// dependan directamente del repositorio legado de movimientos.
/// </summary>
public sealed class VentaKardexMovimientoRegistrar
{
    private readonly IKardexMovimientoWriter _writer;

    public VentaKardexMovimientoRegistrar(IKardexMovimientoWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public Task RegistrarConfirmacionAsync(int ventaId, MovimientoInventario movimiento)
    {
        Validar(ventaId, movimiento);
        return _writer.RegistrarCorrelacionadoAsync(
            movimiento,
            OrigenMovimientoInventario.DesdeVenta(ventaId),
            KardexCorrelationId.VentaConfirmar(ventaId));
    }

    public Task RegistrarAnulacionAsync(int ventaId, MovimientoInventario movimiento)
    {
        Validar(ventaId, movimiento);
        return _writer.RegistrarCorrelacionadoAsync(
            movimiento,
            OrigenMovimientoInventario.DesdeVenta(ventaId),
            KardexCorrelationId.VentaAnular(ventaId));
    }

    private static void Validar(int ventaId, MovimientoInventario movimiento)
    {
        if (ventaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ventaId), "La venta debe estar persistida antes de registrar Kardex.");

        ArgumentNullException.ThrowIfNull(movimiento);
    }
}
