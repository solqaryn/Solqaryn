using System.Collections.Concurrent;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;

namespace InventoryApp.Application.Services;

/// <summary>
/// Proyecta el dominio existente de Descuentos hacia la tienda pública sin crear
/// una segunda fuente de verdad de promociones.
/// </summary>
public sealed class PromocionPublicaService : IPromocionPublicaService
{
    private readonly IDescuentoRepository _repository;
    private readonly SemaphoreSlim _cargaLock = new(1, 1);
    private readonly ConcurrentDictionary<int, Task<int>> _usos = new();
    private List<Descuento>? _vigentes;

    public PromocionPublicaService(IDescuentoRepository repository) => _repository = repository;

    public async Task<OfertaPublicaDto?> ResolverAsync(
        int productoId,
        int? categoriaId,
        decimal precioNormal,
        DateTime fechaUtc)
    {
        if (productoId <= 0 || precioNormal <= 0)
            return null;

        var vigentes = await ObtenerVigentesAsync(fechaUtc);
        var aplicados = new List<Descuento>();
        decimal descuentoTotal = 0m;

        foreach (var descuento in vigentes.OrderBy(d => d.Prioridad).ThenBy(d => d.Id))
        {
            if (!EsPublicable(descuento, productoId, categoriaId, precioNormal, fechaUtc))
                continue;

            if (descuento.LimiteTotalUsos.HasValue)
            {
                var usos = await _usos.GetOrAdd(descuento.Id, id => _repository.ContarUsosAsync(id));
                if (usos >= descuento.LimiteTotalUsos.Value)
                    continue;
            }

            var monto = Redondear(precioNormal * descuento.Valor / 100m);
            var restante = Redondear(precioNormal - descuentoTotal);

            // El checkout publico vigente requiere precio unitario positivo.
            if (monto <= 0 || monto >= restante)
                continue;

            descuentoTotal = Redondear(descuentoTotal + monto);
            aplicados.Add(descuento);

            if (!descuento.Acumulable)
                break;
        }

        if (aplicados.Count == 0 || descuentoTotal <= 0 || descuentoTotal >= precioNormal)
            return null;

        var precioOferta = Redondear(precioNormal - descuentoTotal);
        if (precioOferta <= 0 || precioOferta >= precioNormal)
            return null;

        return new OfertaPublicaDto
        {
            PrecioNormal = Redondear(precioNormal),
            PrecioOferta = precioOferta,
            Ahorro = Redondear(precioNormal - precioOferta),
            PorcentajeAhorro = Redondear((precioNormal - precioOferta) * 100m / precioNormal),
            Nombre = string.Join(" + ", aplicados.Select(d => d.Nombre).Where(n => !string.IsNullOrWhiteSpace(n))),
            VigenteDesdeUtc = aplicados.Where(d => d.FechaInicio.HasValue).Select(d => d.FechaInicio).Max(),
            VigenteHastaUtc = aplicados.Where(d => d.FechaFin.HasValue).Select(d => d.FechaFin).Min()
        };
    }

    private async Task<List<Descuento>> ObtenerVigentesAsync(DateTime fechaUtc)
    {
        if (_vigentes is not null)
            return _vigentes;

        await _cargaLock.WaitAsync();
        try
        {
            _vigentes ??= await _repository.GetVigentesConRelacionesAsync(fechaUtc);
            return _vigentes;
        }
        finally
        {
            _cargaLock.Release();
        }
    }

    private static bool EsPublicable(
        Descuento descuento,
        int productoId,
        int? categoriaId,
        decimal precioNormal,
        DateTime fechaUtc)
    {
        if (!descuento.Activo || descuento.Eliminado)
            return false;
        if (descuento.FechaInicio.HasValue && descuento.FechaInicio.Value > fechaUtc)
            return false;
        if (descuento.FechaFin.HasValue && descuento.FechaFin.Value < fechaUtc)
            return false;

        // Solo reglas que producen el mismo precio unitario para un comprador anonimo.
        if (!string.IsNullOrWhiteSpace(descuento.CodigoPromocionalNormalizado)
            || descuento.RequiereAprobacion
            || descuento.Clientes.Count > 0
            || descuento.Roles.Count > 0
            || descuento.LimiteUsosPorCliente.HasValue
            || descuento.Tipo != TipoDescuento.Porcentaje
            || descuento.Valor <= 0
            || descuento.Valor >= 100
            || descuento.MontoMaximoDescuento.HasValue
            || descuento.CantidadMinima is > 1
            || (descuento.MontoMinimo.HasValue && precioNormal < descuento.MontoMinimo.Value))
        {
            return false;
        }

        if (descuento.Productos.Count == 0 && descuento.Categorias.Count == 0)
            return true;

        return descuento.Productos.Any(p => p.ProductoId == productoId)
            || (categoriaId.HasValue && descuento.Categorias.Any(c => c.CategoriaId == categoriaId.Value));
    }

    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
