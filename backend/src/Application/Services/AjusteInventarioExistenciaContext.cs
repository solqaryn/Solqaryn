using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public static class AjusteInventarioExistenciaContext
{
    public static InventarioDemandaExistencia CrearDemanda(AjusteInventarioDetalle detalle, int cantidad = 1)
    {
        if (!detalle.ProductoVarianteId.HasValue || detalle.ProductoVarianteId.Value <= 0)
            throw new BusinessRuleException("El ajuste físico debe identificar una variante concreta.");
        if (!detalle.AlmacenId.HasValue || detalle.AlmacenId.Value <= 0)
            throw new BusinessRuleException("El ajuste físico debe identificar un almacén válido.");
        if (detalle.UbicacionAlmacenId.HasValue && detalle.UbicacionAlmacenId.Value <= 0)
            throw new BusinessRuleException("La ubicación del ajuste debe ser válida cuando se informa.");
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad de lock debe ser positiva.");

        return new InventarioDemandaExistencia(
            detalle.ProductoId,
            detalle.ProductoVarianteId.Value,
            detalle.AlmacenId.Value,
            detalle.UbicacionAlmacenId,
            cantidad);
    }

    public static InventarioExistenciaClave CrearClave(AjusteInventarioDetalle detalle) =>
        CrearDemanda(detalle).Clave;
}
