using Solqaryn.Domain.Common;

namespace Solqaryn.Application.Common;

/// <summary>
/// Genera identificadores de correlación durables para agrupar todos los
/// movimientos de inventario producidos por una misma operación empresarial.
/// El consumidor debe crear el identificador una sola vez por transacción y
/// reutilizarlo en cada renglón de Kardex de esa operación.
/// </summary>
public static class MovimientoInventarioCorrelationId
{
    public static string NuevaCompra(int compraId) => Crear("compra", compraId);
    public static string NuevaVenta(int ventaId) => Crear("venta", ventaId);
    public static string NuevoConsumo(int consumoInsumoId) => Crear("consumo", consumoInsumoId);
    public static string NuevoAjuste(int ajusteInventarioId) => Crear("ajuste", ajusteInventarioId);

    private static string Crear(string origen, int origenId)
    {
        if (origenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(origenId), "El identificador de origen debe ser positivo.");

        var correlationId = $"{origen}:{origenId}:{Guid.NewGuid():N}";
        if (correlationId.Length > ContextoFisicoMovimientoInventario.MaxCorrelationIdLength)
        {
            throw new InvalidOperationException(
                $"El CorrelationId generado excede {ContextoFisicoMovimientoInventario.MaxCorrelationIdLength} caracteres.");
        }

        return correlationId;
    }
}
