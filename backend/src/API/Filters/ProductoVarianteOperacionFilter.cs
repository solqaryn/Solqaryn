using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Solqaryn.API.Filters;

/// <summary>
/// Impide que una compra o venta utilice el stock general cuando el producto
/// ya posee variantes. También valida pertenencia y estado activo antes de
/// crear, editar, calcular o confirmar un documento.
/// </summary>
public sealed class ProductoVarianteOperacionFilter : IAsyncActionFilter
{
    private readonly IProductoVarianteRepository _varianteRepository;
    private readonly ICompraRepository _compraRepository;
    private readonly IVentaRepository _ventaRepository;

    public ProductoVarianteOperacionFilter(
        IProductoVarianteRepository varianteRepository,
        ICompraRepository compraRepository,
        IVentaRepository ventaRepository)
    {
        _varianteRepository = varianteRepository;
        _compraRepository = compraRepository;
        _ventaRepository = ventaRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argumento in context.ActionArguments.Values)
        {
            switch (argumento)
            {
                case CreateVentaDto venta:
                    await ValidarAsync(venta.Detalles.Select(d => (d.ProductoId, d.ProductoVarianteId)));
                    break;
                case CreateCompraDto compra:
                    await ValidarAsync(compra.Detalles.Select(d => (d.ProductoId, d.ProductoVarianteId)));
                    break;
                case CalcularVentaRequest calculoVenta:
                    await ValidarAsync(calculoVenta.Detalles.Select(d => (d.ProductoId, d.ProductoVarianteId)));
                    break;
                case CalcularCompraRequest calculoCompra:
                    await ValidarAsync(calculoCompra.Detalles.Select(d => (d.ProductoId, d.ProductoVarianteId)));
                    break;
            }
        }

        if (context.ActionDescriptor is ControllerActionDescriptor descriptor &&
            descriptor.ActionName.Equals("Confirmar", StringComparison.OrdinalIgnoreCase) &&
            context.ActionArguments.TryGetValue("id", out var idArgument) &&
            idArgument is int id)
        {
            if (descriptor.ControllerName.Equals("Ventas", StringComparison.OrdinalIgnoreCase))
            {
                var venta = await _ventaRepository.GetByIdAsync(id);
                if (venta is not null)
                    await ValidarAsync(venta.Detalles.Select(d => (d.ProductoId, d.ProductoVarianteId)));
            }
            else if (descriptor.ControllerName.Equals("Compras", StringComparison.OrdinalIgnoreCase))
            {
                var compra = await _compraRepository.GetByIdAsync(id);
                if (compra is not null)
                    await ValidarAsync(compra.Detalles.Select(d => (d.ProductoId, d.ProductoVarianteId)));
            }
        }

        await next();
    }

    private async Task ValidarAsync(IEnumerable<(int ProductoId, int? ProductoVarianteId)> detalles)
    {
        foreach (var detalle in detalles.Distinct())
        {
            var variantes = await _varianteRepository.GetByProductoIdAsync(detalle.ProductoId, incluirInactivas: true);
            if (variantes.Count == 0)
                continue;

            if (!detalle.ProductoVarianteId.HasValue)
                throw new BusinessRuleException("Debes seleccionar una variante activa para cada producto que tenga colores o SKU administrados.");

            var variante = variantes.FirstOrDefault(v => v.Id == detalle.ProductoVarianteId.Value);
            if (variante is null)
                throw new BusinessRuleException("La variante seleccionada no pertenece al producto indicado.");

            if (!variante.Activo)
                throw new BusinessRuleException($"La variante '{variante.Sku}' está inactiva y no puede utilizarse en nuevas operaciones.");
        }
    }
}
