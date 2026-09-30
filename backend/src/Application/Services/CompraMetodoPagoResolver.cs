using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Services;

/// <summary>
/// Centraliza la transición de Compras hacia MetodoPagoId como autoridad relacional.
/// La proyección al enum legacy existe únicamente como bridge temporal para contratos
/// que todavía no han sido retirados por ERP-N0.
/// </summary>
public static class CompraMetodoPagoResolver
{
    public static async Task<CatalogoMetodoPago> ResolverAsync(
        ICompraRepository compraRepository,
        string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException("El método de pago es obligatorio.");

        var normalizado = valor.Trim();
        var metodoPago = await compraRepository.GetMetodoPagoPorCodigoONombreAsync(normalizado);

        return metodoPago
            ?? throw new BusinessRuleException(
                $"El método de pago '{normalizado}' no existe en el catálogo.");
    }

    public static MetodoPago DerivarLegacy(CatalogoMetodoPago metodoPago)
    {
        ArgumentNullException.ThrowIfNull(metodoPago);

        if (Enum.TryParse<MetodoPago>(metodoPago.Codigo, true, out var porCodigo))
            return porCodigo;
        if (Enum.TryParse<MetodoPago>(metodoPago.Nombre, true, out var porNombre))
            return porNombre;

        // Compatibilidad transitoria: la FK MetodoPagoId sigue siendo la autoridad.
        return MetodoPago.Otro;
    }
}
