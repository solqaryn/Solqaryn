using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

/// <summary>
/// Distribuye los componentes monetarios ya persistidos en el encabezado de una
/// factura nueva entre sus líneas. No consulta configuraciones actuales ni
/// recalcula reglas fiscales: trabaja exclusivamente con el snapshot del documento.
/// </summary>
public static class FacturaDetalleDistribuidor
{
    public static void Aplicar(Factura factura)
    {
        ArgumentNullException.ThrowIfNull(factura);

        var detalles = factura.Detalles.ToList();
        if (detalles.Count == 0)
            return;

        var importeBruto = Redondear(factura.ImporteBruto > 0
            ? factura.ImporteBruto
            : detalles.Sum(d => Redondear(d.Subtotal)));
        var envio = Redondear(Math.Max(0, factura.CostoEnvio));
        var descuento = Redondear(Math.Max(0, factura.Descuento));
        var impuestoTotal = Redondear(Math.Max(0, factura.Impuesto));
        var totalDocumento = Redondear(Math.Max(0, factura.Total));

        if (envio > importeBruto)
            throw new BusinessRuleException("El costo de envío no puede superar el importe bruto de la factura.");

        // El importe bruto actual de Solqaryn contiene los productos y el envío.
        // El impuesto adicional es exactamente la diferencia que queda fuera del
        // importe bruto después de aplicar el descuento. El resto del impuesto
        // pertenece al precio incluido y, por tanto, ya está dentro del producto.
        var impuestoAdicional = Redondear(Math.Max(0, totalDocumento - importeBruto + descuento));
        if (impuestoAdicional > impuestoTotal)
            throw new BusinessRuleException("El snapshot de impuestos de la factura no concilia con su total.");

        var impuestoIncluido = Redondear(impuestoTotal - impuestoAdicional);
        var importeProductos = Redondear(Math.Max(0, importeBruto - envio));

        var pesosBrutos = detalles
            .Select(d => Redondear(Math.Max(0, d.Subtotal)))
            .ToArray();
        var productosPorLinea = Distribuir(importeProductos, pesosBrutos);
        var descuentosPorLinea = Distribuir(descuento, productosPorLinea);
        var impuestosIncluidosPorLinea = Distribuir(impuestoIncluido, productosPorLinea);
        var pesosImpuestoAdicional = productosPorLinea
            .Select((monto, i) => Redondear(Math.Max(0, monto - descuentosPorLinea[i])))
            .ToArray();
        var impuestosAdicionalesPorLinea = Distribuir(impuestoAdicional, pesosImpuestoAdicional);

        for (var i = 0; i < detalles.Count; i++)
        {
            var detalle = detalles[i];
            detalle.Descuento = descuentosPorLinea[i];
            detalle.Impuesto = Redondear(impuestosIncluidosPorLinea[i] + impuestosAdicionalesPorLinea[i]);
            detalle.TotalLinea = Redondear(
                productosPorLinea[i] - descuentosPorLinea[i] + impuestosAdicionalesPorLinea[i]);
        }

        ValidarConciliacion(factura, detalles, descuento, impuestoTotal, envio, totalDocumento);
    }

    private static decimal[] Distribuir(decimal total, IReadOnlyList<decimal> pesos)
    {
        total = Redondear(Math.Max(0, total));
        var resultado = new decimal[pesos.Count];
        if (pesos.Count == 0 || total == 0)
            return resultado;

        var pesosNormalizados = pesos.Select(p => Redondear(Math.Max(0, p))).ToArray();
        var sumaPesos = pesosNormalizados.Sum();

        if (sumaPesos <= 0)
        {
            resultado[0] = total;
            return resultado;
        }

        for (var i = 0; i < pesosNormalizados.Length; i++)
            resultado[i] = Redondear(total * pesosNormalizados[i] / sumaPesos);

        var residuo = Redondear(total - resultado.Sum());
        if (residuo != 0)
        {
            // Regla determinista: el residuo se aplica a la línea con mayor monto
            // neto/base. En empate gana el menor índice (orden original estable).
            var indice = Enumerable.Range(0, pesosNormalizados.Length)
                .OrderByDescending(i => pesosNormalizados[i])
                .ThenBy(i => i)
                .First();
            resultado[indice] = Redondear(resultado[indice] + residuo);
        }

        if (Redondear(resultado.Sum()) != total)
            throw new BusinessRuleException("No fue posible distribuir el importe de factura al centavo.");

        return resultado;
    }

    private static void ValidarConciliacion(
        Factura factura,
        IReadOnlyCollection<FacturaDetalle> detalles,
        decimal descuento,
        decimal impuesto,
        decimal envio,
        decimal totalDocumento)
    {
        var descuentoLineas = Redondear(detalles.Sum(d => d.Descuento));
        var impuestoLineas = Redondear(detalles.Sum(d => d.Impuesto));
        var totalLineasConEnvio = Redondear(detalles.Sum(d => d.TotalLinea) + envio);

        if (descuentoLineas != descuento)
            throw new BusinessRuleException("El descuento de las líneas no concilia con el encabezado de la factura.");
        if (impuestoLineas != impuesto)
            throw new BusinessRuleException("El impuesto de las líneas no concilia con el encabezado de la factura.");
        if (totalLineasConEnvio != totalDocumento)
            throw new BusinessRuleException("El total de las líneas no concilia con el total de la factura.");

        // Protección adicional para evitar persistir un documento negativo o
        // modificar silenciosamente un snapshot que ya viene calculado del backend.
        if (factura.Total < 0)
            throw new BusinessRuleException("El total de la factura no puede ser negativo.");
    }

    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}