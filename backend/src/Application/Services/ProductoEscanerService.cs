using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class ProductoEscanerService : IProductoEscanerService
{
    private const int LongitudMaximaCodigo = 120;
    private const int LongitudMinimaBusqueda = 2;
    private const int LongitudMaximaBusqueda = 100;
    private const int LimiteMaximoBusqueda = 30;
    private readonly IProductoVarianteRepository _varianteRepository;

    public ProductoEscanerService(IProductoVarianteRepository varianteRepository)
    {
        _varianteRepository = varianteRepository;
    }

    public async Task<ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>> ResolverParaVentaAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var resolucion = await ResolverVarianteAsync(codigo, cancellationToken);
        if (resolucion.Estado != EstadoResolucionProductoEscaner.Encontrado)
            return ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>.Fallo(resolucion.Estado, resolucion.Mensaje);

        var variante = resolucion.Dato!;
        if (variante.Producto.TipoInventario != TipoInventario.MercaderiaVenta)
            return ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>.Fallo(EstadoResolucionProductoEscaner.NoOperativo, "El código corresponde a un insumo administrativo y no puede venderse.");
        if (variante.Cantidad <= 0)
            return ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>.Fallo(EstadoResolucionProductoEscaner.NoOperativo, "La variante escaneada no tiene existencias disponibles para la venta.");

        return ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto>.Encontrado(MapVenta(variante));
    }

    public async Task<ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto>> ResolverParaCompraAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var resolucion = await ResolverVarianteAsync(codigo, cancellationToken);
        if (resolucion.Estado != EstadoResolucionProductoEscaner.Encontrado)
            return ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto>.Fallo(resolucion.Estado, resolucion.Mensaje);
        return ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto>.Encontrado(MapCompra(resolucion.Dato!));
    }

    public async Task<List<ProductoEscaneadoVentaDto>> BuscarParaVentaAsync(string termino, int limite = LimiteMaximoBusqueda, CancellationToken cancellationToken = default)
    {
        var terminoNormalizado = NormalizarTerminoBusqueda(termino);
        var variantes = await _varianteRepository.BuscarPorTerminoAsync(terminoNormalizado, true, Math.Clamp(limite, 1, LimiteMaximoBusqueda), cancellationToken, TipoInventario.MercaderiaVenta);
        return variantes.Select(MapVenta).ToList();
    }

    public async Task<List<ProductoEscaneadoCompraDto>> BuscarParaCompraAsync(string termino, int limite = LimiteMaximoBusqueda, CancellationToken cancellationToken = default)
    {
        var terminoNormalizado = NormalizarTerminoBusqueda(termino);
        var variantes = await _varianteRepository.BuscarPorTerminoAsync(terminoNormalizado, false, Math.Clamp(limite, 1, LimiteMaximoBusqueda), cancellationToken);
        return variantes.Select(MapCompra).ToList();
    }

    private async Task<ResultadoResolucionProductoEscaner<ProductoVariante>> ResolverVarianteAsync(string codigo, CancellationToken cancellationToken)
    {
        var codigoNormalizado = codigo?.Trim() ?? string.Empty;
        if (codigoNormalizado.Length == 0)
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(EstadoResolucionProductoEscaner.EntradaInvalida, "Ingresa un SKU o código de barras.");
        if (codigoNormalizado.Length > LongitudMaximaCodigo)
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(EstadoResolucionProductoEscaner.EntradaInvalida, $"El código no puede superar {LongitudMaximaCodigo} caracteres.");

        var coincidencias = await _varianteRepository.BuscarPorCodigoAsync(codigoNormalizado.ToUpperInvariant(), codigoNormalizado, cancellationToken);
        if (coincidencias.Count == 0)
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(EstadoResolucionProductoEscaner.NoEncontrado, "No se encontró una variante con el SKU o código de barras indicado.");
        if (coincidencias.Count > 1)
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(EstadoResolucionProductoEscaner.Conflicto, "El código coincide con más de una variante. Corrige los identificadores antes de continuar.");

        var variante = coincidencias[0];
        if (variante.Eliminado || !variante.Activo || variante.Producto is null || variante.Producto.Eliminado || !variante.Producto.Activo)
            return ResultadoResolucionProductoEscaner<ProductoVariante>.Fallo(EstadoResolucionProductoEscaner.NoOperativo, "El producto o su variante están inactivos y no pueden utilizarse en esta operación.");
        return ResultadoResolucionProductoEscaner<ProductoVariante>.Encontrado(variante);
    }

    private static string NormalizarTerminoBusqueda(string termino)
    {
        var normalizado = termino?.Trim() ?? string.Empty;
        if (normalizado.Length < LongitudMinimaBusqueda)
            throw new BusinessRuleException($"Escribe al menos {LongitudMinimaBusqueda} caracteres para buscar productos.");
        if (normalizado.Length > LongitudMaximaBusqueda)
            throw new BusinessRuleException($"La búsqueda no puede superar {LongitudMaximaBusqueda} caracteres.");
        return normalizado.ToLowerInvariant();
    }

    private static string? ObtenerImagenMiniatura(Producto producto) =>
        producto.Imagenes
            .OrderByDescending(imagen => imagen.EsPrincipal)
            .ThenBy(imagen => imagen.Orden)
            .Select(imagen => imagen.Url)
            .FirstOrDefault();

    private static string ConstruirEtiqueta(ProductoVariante variante)
    {
        var partes = new[] { variante.Marca?.Nombre, variante.Modelo?.Nombre, variante.Color?.Nombre, variante.Talla?.Nombre, variante.Sku };
        return string.Join(" · ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
    }

    private static ProductoEscaneadoVentaDto MapVenta(ProductoVariante variante) => new()
    {
        ProductoId = variante.ProductoId, ProductoVarianteId = variante.Id, ProductoNombre = variante.Producto.Nombre,
        Marca = variante.Marca?.Nombre ?? string.Empty, Modelo = variante.Modelo?.Nombre ?? string.Empty,
        MarcaId = variante.MarcaId, MarcaNombre = variante.Marca?.Nombre, ModeloId = variante.ModeloId, ModeloNombre = variante.Modelo?.Nombre,
        ColorId = variante.ColorId, ColorNombre = variante.Color?.Nombre, TallaId = variante.TallaId, TallaNombre = variante.Talla?.Nombre,
        Etiqueta = ConstruirEtiqueta(variante), EsVarianteTecnica = variante.EsTecnica, Sku = variante.Sku ?? string.Empty,
        CodigoBarras = variante.CodigoBarras, CantidadDisponible = variante.Cantidad, Precio = variante.Precio ?? 0m,
        ImagenMiniaturaUrl = ObtenerImagenMiniatura(variante.Producto)
    };

    private static ProductoEscaneadoCompraDto MapCompra(ProductoVariante variante) => new()
    {
        ProductoId = variante.ProductoId, ProductoVarianteId = variante.Id, ProductoNombre = variante.Producto.Nombre,
        Marca = variante.Marca?.Nombre ?? string.Empty, Modelo = variante.Modelo?.Nombre ?? string.Empty,
        MarcaId = variante.MarcaId, MarcaNombre = variante.Marca?.Nombre, ModeloId = variante.ModeloId, ModeloNombre = variante.Modelo?.Nombre,
        ColorId = variante.ColorId, ColorNombre = variante.Color?.Nombre, TallaId = variante.TallaId, TallaNombre = variante.Talla?.Nombre,
        Etiqueta = ConstruirEtiqueta(variante), EsVarianteTecnica = variante.EsTecnica, Sku = variante.Sku ?? string.Empty,
        CodigoBarras = variante.CodigoBarras, CantidadDisponible = variante.Cantidad, Costo = variante.Costo ?? 0m,
        Precio = variante.Precio ?? 0m, ImagenMiniaturaUrl = ObtenerImagenMiniatura(variante.Producto)
    };
}
