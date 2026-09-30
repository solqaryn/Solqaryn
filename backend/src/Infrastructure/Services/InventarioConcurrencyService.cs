using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Solqaryn.Infrastructure.Services;

public class InventarioConcurrencyService : IInventarioConcurrencyService
{
    private readonly AppDbContext _context;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;

    public InventarioConcurrencyService(
        AppDbContext context,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository)
    {
        _context = context;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
    }

    public Task<InventarioLockSet> BloquearYValidarInventarioAsync(
        IEnumerable<InventarioDemanda> demandMap,
        bool esDeduccion = true) =>
        BloquearYValidarCoreAsync(demandMap, esDeduccion, incluirEliminados: false);

    public Task<InventarioLockSet> BloquearInventarioParaReversionAsync(
        IEnumerable<InventarioDemanda> demandMap) =>
        BloquearYValidarCoreAsync(demandMap, esDeduccion: false, incluirEliminados: true);

    private async Task<InventarioLockSet> BloquearYValidarCoreAsync(
        IEnumerable<InventarioDemanda> demandMap,
        bool esDeduccion,
        bool incluirEliminados)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El bloqueo de inventario requiere una transacción activa.");

        var consolidada = demandMap
            .Select(x => x ?? throw new ArgumentException("La demanda de inventario contiene un elemento nulo.", nameof(demandMap)))
            .GroupBy(x => (x.ProductoId, x.ProductoVarianteId))
            .Select(g => new InventarioDemanda(
                g.Key.ProductoId,
                g.Key.ProductoVarianteId,
                g.Sum(x => x.Cantidad)))
            .OrderBy(x => x.ProductoId)
            .ThenBy(x => x.ProductoVarianteId)
            .ToList();

        if (consolidada.Any(x => x.ProductoId <= 0 || x.Cantidad <= 0))
            throw new BusinessRuleException("Cada demanda de inventario debe indicar un producto válido y una cantidad mayor a cero.");

        if (consolidada.Count == 0)
        {
            return new InventarioLockSet(
                new Dictionary<int, Producto>(),
                new Dictionary<int, ProductoVariante>(),
                Array.Empty<InventarioDemanda>());
        }

        var productoIds = consolidada
            .Select(x => x.ProductoId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var productos = incluirEliminados
            ? await BloquearProductosIncluyendoEliminadosAsync(productoIds)
            : await _productoRepository.GetByIdsForUpdateAsync(productoIds);
        var productosMap = productos.ToDictionary(p => p.Id);

        var varianteIds = consolidada
            .Where(x => x.ProductoVarianteId.HasValue)
            .Select(x => x.ProductoVarianteId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var variantes = incluirEliminados
            ? await BloquearVariantesIncluyendoEliminadasAsync(varianteIds)
            : await _productoVarianteRepository.GetByIdsForUpdateAsync(varianteIds);
        var variantesMap = variantes.ToDictionary(v => v.Id);

        foreach (var productoGrupo in consolidada.GroupBy(x => x.ProductoId))
        {
            if (!productosMap.TryGetValue(productoGrupo.Key, out var producto))
                throw new BusinessRuleException($"El producto ID '{productoGrupo.Key}' no existe físicamente.");

            var demandasLegacySinVariante = productoGrupo.Where(x => !x.ProductoVarianteId.HasValue).ToList();
            var cantidadLegacy = demandasLegacySinVariante.Sum(x => x.Cantidad);
            if (esDeduccion && cantidadLegacy > 0 && producto.Cantidad < cantidadLegacy)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente para '{producto.Nombre}': disponible {producto.Cantidad}, solicitado {cantidadLegacy}.");
            }
        }

        foreach (var item in consolidada)
        {
            var producto = productosMap[item.ProductoId];

            if (item.ProductoVarianteId.HasValue)
            {
                if (!variantesMap.TryGetValue(item.ProductoVarianteId.Value, out var variante))
                    throw new BusinessRuleException($"La variante ID '{item.ProductoVarianteId.Value}' no existe físicamente.");

                if (variante.ProductoId != producto.Id)
                    throw new BusinessRuleException($"La variante ID '{variante.Id}' no pertenece al producto ID '{producto.Id}'.");

                if (esDeduccion && variante.Cantidad < item.Cantidad)
                {
                    throw new BusinessRuleException(
                        $"Stock insuficiente para la variante '{variante.Sku}': disponible {variante.Cantidad}, solicitado {item.Cantidad}.");
                }
            }
        }

        return new InventarioLockSet(productosMap, variantesMap, consolidada);
    }

    private async Task<List<Producto>> BloquearProductosIncluyendoEliminadosAsync(IEnumerable<int> ids)
    {
        var resultado = new List<Producto>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var producto = await _context.Productos
                .FromSqlInterpolated($"SELECT p.* FROM Productos p WHERE p.Id = {id} FOR UPDATE")
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync();
            if (producto is not null)
                resultado.Add(producto);
        }
        return resultado;
    }

    private async Task<List<ProductoVariante>> BloquearVariantesIncluyendoEliminadasAsync(IEnumerable<int> ids)
    {
        var resultado = new List<ProductoVariante>();
        foreach (var id in ids.Distinct().OrderBy(x => x))
        {
            var variante = await _context.ProductoVariantes
                .FromSqlInterpolated($"SELECT pv.* FROM ProductoVariantes pv WHERE pv.Id = {id} FOR UPDATE")
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync();
            if (variante is not null)
                resultado.Add(variante);
        }
        return resultado;
    }

    public async Task AjustarStockPesimistaAsync(
        int productoId,
        int? productoVarianteId,
        int cantidadActualEsperada,
        int cantidadNueva)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("AjustarStockPesimistaAsync requiere una transacción activa.");

        if (cantidadActualEsperada < 0 || cantidadNueva < 0)
            throw new BusinessRuleException("Las cantidades de inventario no pueden ser negativas.");

        var producto = await _productoRepository.GetByIdForUpdateAsync(productoId)
            ?? throw new BusinessRuleException($"El producto ID '{productoId}' no existe.");

        if (productoVarianteId.HasValue)
        {
            var variante = await _productoVarianteRepository.GetByIdForUpdateAsync(productoVarianteId.Value)
                ?? throw new BusinessRuleException($"La variante ID '{productoVarianteId.Value}' no existe.");

            if (variante.ProductoId != productoId)
                throw new BusinessRuleException("La variante indicada no pertenece al producto solicitado.");

            if (variante.Cantidad != cantidadActualEsperada)
            {
                throw new BusinessRuleException(
                    "El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");
            }

            variante.Cantidad = cantidadNueva;
            _productoVarianteRepository.Update(variante);

            var todasVariantes = await _productoVarianteRepository.GetByProductoIdAsync(productoId, incluirInactivas: true);
            producto.Cantidad = todasVariantes.Sum(v => v.Cantidad);
            _productoRepository.Update(producto);
            return;
        }

        var variantesExistentes = await _productoVarianteRepository
            .GetByProductoIdAsync(productoId, incluirInactivas: true);
        var tecnica = variantesExistentes.SingleOrDefault(v => v.EsTecnica);
        var comerciales = variantesExistentes.Where(v => !v.EsTecnica).ToList();

        if (tecnica is not null && comerciales.Count == 0)
        {
            tecnica = await _productoVarianteRepository.GetByIdForUpdateAsync(tecnica.Id) ?? tecnica;
            if (tecnica.Cantidad != cantidadActualEsperada)
            {
                throw new BusinessRuleException(
                    "El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");
            }

            tecnica.Cantidad = cantidadNueva;
            _productoVarianteRepository.Update(tecnica);
            producto.Cantidad = cantidadNueva;
            _productoRepository.Update(producto);
            return;
        }

        if (variantesExistentes.Count > 0)
        {
            throw new BusinessRuleException(
                "El producto tiene variantes comerciales. Ajusta el inventario de cada variante; el stock total se recalcula automáticamente.");
        }

        // Compatibilidad previa al backfill N0.1: solo se ejecuta si aún no existe ninguna variante.
        if (producto.Cantidad != cantidadActualEsperada)
        {
            throw new BusinessRuleException(
                "El inventario cambió desde que se cargó el formulario. Actualiza los datos e inténtalo nuevamente.");
        }

        producto.Cantidad = cantidadNueva;
        _productoRepository.Update(producto);
    }
}
