using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public static class TransferenciaInventarioExistenciaContext
{
    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasDespacho(
        TransferenciaInventario transferencia) =>
        ConstruirDemandasDespachadas(transferencia, destino: false);

    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasTransitoDestino(
        TransferenciaInventario transferencia) =>
        ConstruirDemandasDespachadas(transferencia, destino: true);

    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasBloqueoDespacho(
        TransferenciaInventario transferencia) =>
        ConstruirDemandasDespacho(transferencia)
            .Concat(ConstruirDemandasTransitoDestino(transferencia))
            .ToList();

    /// <summary>
    /// Demanda usada para cerrar el tránsito: siempre representa lo despachado,
    /// incluso cuando un detalle termina totalmente faltante o dañado.
    /// </summary>
    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasCierreTransitoDestino(
        TransferenciaInventario transferencia) =>
        ConstruirDemandasTransitoDestino(transferencia);

    /// <summary>
    /// Materializa únicamente unidades realmente recibidas como stock físico.
    /// Faltantes y dañadas no entran al disponible; sobrantes permanecen como
    /// discrepancia explícita hasta una operación empresarial de aceptación.
    /// </summary>
    public static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasIngresoDestino(
        TransferenciaInventario transferencia)
    {
        ValidarBase(transferencia, destino: true);
        var demandas = new List<InventarioDemandaExistencia>();
        foreach (var detalle in transferencia.Detalles)
        {
            ValidarVariante(detalle);
            if (detalle.CantidadRecibida <= 0)
                continue;

            demandas.Add(CrearDemanda(
                transferencia.AlmacenDestinoId,
                detalle.UbicacionDestinoId,
                detalle,
                detalle.CantidadRecibida));
        }

        return demandas;
    }

    private static IReadOnlyList<InventarioDemandaExistencia> ConstruirDemandasDespachadas(
        TransferenciaInventario transferencia,
        bool destino)
    {
        ValidarBase(transferencia, destino);
        var almacenId = destino ? transferencia.AlmacenDestinoId : transferencia.AlmacenOrigenId;
        var demandas = new List<InventarioDemandaExistencia>(transferencia.Detalles.Count);
        foreach (var detalle in transferencia.Detalles)
        {
            ValidarVariante(detalle);
            if (detalle.CantidadDespachada <= 0)
                throw new BusinessRuleException("Cada detalle debe tener una cantidad despachada mayor que cero.");

            demandas.Add(CrearDemanda(
                almacenId,
                destino ? detalle.UbicacionDestinoId : detalle.UbicacionOrigenId,
                detalle,
                detalle.CantidadDespachada));
        }

        return demandas;
    }

    private static void ValidarBase(TransferenciaInventario transferencia, bool destino)
    {
        ArgumentNullException.ThrowIfNull(transferencia);
        var almacenId = destino ? transferencia.AlmacenDestinoId : transferencia.AlmacenOrigenId;
        if (almacenId <= 0)
            throw new BusinessRuleException($"La transferencia no tiene un almacén de {(destino ? "destino" : "origen")} válido.");
        if (transferencia.Detalles.Count == 0)
            throw new BusinessRuleException("La transferencia no contiene detalles físicos.");
    }

    private static void ValidarVariante(TransferenciaInventarioDetalle detalle)
    {
        if (detalle.ProductoVarianteId <= 0 || detalle.ProductoVariante is null || detalle.ProductoVariante.ProductoId <= 0)
            throw new BusinessRuleException("Cada detalle debe tener una variante cargada con producto válido.");
    }

    private static InventarioDemandaExistencia CrearDemanda(
        int almacenId,
        int? ubicacionId,
        TransferenciaInventarioDetalle detalle,
        int cantidad) =>
        new(
            detalle.ProductoVariante.ProductoId,
            detalle.ProductoVarianteId,
            almacenId,
            ubicacionId,
            cantidad);
}
