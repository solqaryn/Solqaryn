using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

/// <summary>
/// Operaciones puras de adaptación entre un ajuste formal y la autoridad física
/// ExistenciaVariante. Mantiene la construcción de claves, orden de locks y
/// validaciones del cutover N1.4.D fuera del servicio orquestador.
/// </summary>
public static class AjusteInventarioExistenciaStock
{
    public static IReadOnlyList<InventarioDemandaExistencia> CrearDemandas(
        IEnumerable<AjusteInventarioDetalle> detalles)
    {
        ArgumentNullException.ThrowIfNull(detalles);

        return detalles
            .Select(detalle => AjusteInventarioExistenciaContext.CrearDemanda(detalle))
            .OrderBy(d => d.ProductoVarianteId)
            .ThenBy(d => d.AlmacenId)
            .ThenBy(d => d.UbicacionAlmacenId ?? 0)
            .ToList();
    }

    public static ExistenciaVariante ObtenerExistencia(
        InventarioExistenciaLockSet lockSet,
        AjusteInventarioDetalle detalle)
    {
        ArgumentNullException.ThrowIfNull(lockSet);
        ArgumentNullException.ThrowIfNull(detalle);

        var clave = AjusteInventarioExistenciaContext.CrearClave(detalle);
        if (!lockSet.Existencias.TryGetValue(clave, out var existencia))
        {
            throw new BusinessRuleException(
                $"No existe stock autoritativo bloqueado para variante {clave.ProductoVarianteId}, almacén {clave.AlmacenId} y ubicación {(clave.UbicacionAlmacenId?.ToString() ?? "raíz")}.");
        }

        if (existencia.ProductoVarianteId != detalle.ProductoVarianteId)
            throw new BusinessRuleException("La existencia bloqueada no corresponde a la variante del ajuste.");

        return existencia;
    }

    public static int CalcularDiferencia(int stockFisicoActual, int cantidadObjetivo)
    {
        if (stockFisicoActual < 0)
            throw new BusinessRuleException("El stock físico actual no puede ser negativo.");
        if (cantidadObjetivo < 0)
            throw new BusinessRuleException("La cantidad objetivo no puede ser negativa.");

        return cantidadObjetivo - stockFisicoActual;
    }

    public static void ValidarObjetivoContraReservado(
        ExistenciaVariante existencia,
        int cantidadObjetivo)
    {
        ArgumentNullException.ThrowIfNull(existencia);

        if (cantidadObjetivo < 0)
            throw new BusinessRuleException("La cantidad objetivo no puede ser negativa.");
        if (cantidadObjetivo < existencia.StockReservado)
        {
            throw new BusinessRuleException(
                $"El stock físico objetivo ({cantidadObjetivo}) no puede quedar por debajo del stock reservado ({existencia.StockReservado}).");
        }
    }

    public static int CalcularObjetivoReversion(
        ExistenciaVariante existencia,
        int diferenciaOriginal)
    {
        ArgumentNullException.ThrowIfNull(existencia);

        var objetivo = existencia.StockFisico - diferenciaOriginal;
        if (objetivo < 0)
        {
            throw new BusinessRuleException(
                "La reversión del ajuste dejaría el stock físico autoritativo en negativo.");
        }

        ValidarObjetivoContraReservado(existencia, objetivo);
        return objetivo;
    }
}
