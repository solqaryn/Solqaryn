using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Mappings;

public static class ProductoMapper
{
    public static ProductoDto ToDto(Producto p)
    {
        var variantes = p.Variantes
            .Where(v => !v.Eliminado)
            .OrderBy(v => v.Marca?.Nombre)
            .ThenBy(v => v.Modelo?.Nombre)
            .ThenBy(v => v.Color?.Nombre)
            .ThenBy(v => v.Talla?.Nombre)
            .ThenBy(v => v.Sku)
            .ToList();
        var activas = variantes.Where(v => v.Activo).ToList();
        var operativas = activas.Count > 0 ? activas : variantes;
        var stockTotal = variantes.Sum(v => v.Cantidad);
        var costo = CalcularCosto(variantes);
        var precios = operativas.Where(v => v.Precio.HasValue).Select(v => v.Precio!.Value).ToList();
        var precio = precios.Count > 0 ? precios.Min() : 0m;
        var precioMaximo = precios.Count > 0 ? precios.Max() : 0m;
        var umbral = variantes.Sum(v => v.UmbralStockBajo);
        var agotado = p.Activo && !p.Eliminado && (activas.Count == 0 || activas.All(v => v.Cantidad <= 0));
        var stockBajo = p.Activo && !p.Eliminado && !agotado && activas.Any(v => v.TieneStockBajo);

        var soloTecnicaPreBackfill = variantes.Count == 1 && variantes[0].EsTecnica;
        var marcaId = ValorComun(variantes, v => v.MarcaId) ?? (soloTecnicaPreBackfill ? p.MarcaId : null);
        var modeloId = ValorComun(variantes, v => v.ModeloId) ?? (soloTecnicaPreBackfill ? p.ModeloId : null);
        var colorId = ValorComun(variantes, v => v.ColorId) ?? (soloTecnicaPreBackfill ? p.ColorId : null);
        var tallaId = ValorComun(variantes, v => v.TallaId) ?? (soloTecnicaPreBackfill ? p.TallaId : null);

        var marcaNombres = variantes.Select(v => v.Marca?.Nombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var modeloNombres = variantes.Select(v => v.Modelo?.Nombre).Where(NoVacio).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var marca = marcaNombres.Count > 0 ? string.Join(" / ", marcaNombres!) : (soloTecnicaPreBackfill ? p.MarcaCatalogo?.Nombre ?? p.Marca : string.Empty);
        var modelo = modeloNombres.Count > 0 ? string.Join(" / ", modeloNombres!) : (soloTecnicaPreBackfill ? p.ModeloCatalogo?.Nombre ?? p.Modelo : string.Empty);

        var color = colorId.HasValue ? variantes.FirstOrDefault(v => v.ColorId == colorId)?.Color ?? (soloTecnicaPreBackfill ? p.ColorCatalogo : null) : null;
        var talla = tallaId.HasValue ? variantes.FirstOrDefault(v => v.TallaId == tallaId)?.Talla ?? (soloTecnicaPreBackfill ? p.TallaCatalogo : null) : null;
        var marcaEntidad = marcaId.HasValue ? variantes.FirstOrDefault(v => v.MarcaId == marcaId)?.Marca ?? (soloTecnicaPreBackfill ? p.MarcaCatalogo : null) : null;
        var modeloEntidad = modeloId.HasValue ? variantes.FirstOrDefault(v => v.ModeloId == modeloId)?.Modelo ?? (soloTecnicaPreBackfill ? p.ModeloCatalogo : null) : null;

        var imagenesGenerales = p.Imagenes
            .Where(i => i.ProductoVarianteId == null)
            .OrderBy(i => i.Orden)
            .ToList();

        return new ProductoDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Marca = marca,
            Modelo = modelo,
            Descripcion = p.Descripcion,
            TipoInventario = p.TipoInventario,
            Cantidad = stockTotal,
            Costo = costo,
            Precio = precio,
            PrecioMinimo = precio,
            PrecioMaximo = precioMaximo,
            UmbralStockBajo = umbral,
            TieneStockBajo = stockBajo,
            EstaAgotado = agotado,
            EstadoInventario = agotado ? "Agotado" : stockBajo ? "Stock bajo" : "Disponible",
            Activo = p.Activo,
            EsDestacado = p.EsDestacado,
            CategoriaId = p.CategoriaId,
            CategoriaNombre = p.Categoria?.Nombre,
            ColorId = colorId,
            ColorNombre = color?.Nombre,
            ColorCodigoVisual = color?.CodigoVisual,
            TallaId = tallaId,
            TallaNombre = talla?.Nombre,
            MarcaId = marcaId,
            MarcaNombre = marcaEntidad?.Nombre,
            ModeloId = modeloId,
            ModeloNombre = modeloEntidad?.Nombre,
            ImagenPrincipalUrl = p.ImagenPrincipal?.Url,
            TotalImagenes = imagenesGenerales.Count,
            Imagenes = imagenesGenerales.Select(i => new ProductoImagenDto
            {
                Id = i.Id,
                Url = i.Url,
                Orden = i.Orden,
                EsPrincipal = i.EsPrincipal,
                ProductoVarianteId = null
            }).ToList(),
            Variantes = variantes.Select(v => new ProductoVarianteDto
            {
                Id = v.Id,
                ProductoId = v.ProductoId,
                ProductoNombre = p.Nombre,
                MarcaId = v.MarcaId,
                MarcaNombre = v.Marca?.Nombre,
                ModeloId = v.ModeloId,
                ModeloNombre = v.Modelo?.Nombre,
                ColorId = v.ColorId,
                ColorNombre = v.Color?.Nombre,
                ColorCodigoVisual = v.Color?.CodigoVisual,
                TallaId = v.TallaId,
                TallaNombre = v.Talla?.Nombre,
                Etiqueta = ConstruirEtiqueta(v),
                Sku = v.Sku ?? string.Empty,
                CodigoBarras = v.CodigoBarras,
                Cantidad = v.Cantidad,
                UmbralStockBajo = v.UmbralStockBajo,
                Costo = v.Costo ?? 0m,
                Precio = v.Precio ?? 0m,
                Activo = v.Activo,
                EsTecnica = v.EsTecnica,
                TieneStockBajo = v.TieneStockBajo,
                EstaAgotada = v.EstaAgotada,
                EstadoInventario = v.EstaAgotada ? "Agotada" : v.TieneStockBajo ? "Stock bajo" : "Disponible",
                Imagenes = v.Imagenes.OrderBy(i => i.Orden).Select(i => new ProductoImagenDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    Orden = i.Orden,
                    EsPrincipal = i.EsPrincipal,
                    ProductoVarianteId = v.Id
                }).ToList(),
                FechaCreacion = v.FechaCreacion,
                FechaActualizacion = v.FechaActualizacion
            }).ToList(),
            TotalVariantes = variantes.Count,
            CreadoPorUsuarioId = p.CreadoPorUsuarioId,
            CreadoPorNombreUsuario = p.CreadoPorNombreUsuario,
            ActualizadoPorUsuarioId = p.ActualizadoPorUsuarioId,
            ActualizadoPorNombreUsuario = p.ActualizadoPorNombreUsuario,
            FechaCreacion = p.FechaCreacion,
            FechaActualizacion = p.FechaActualizacion
        };
    }

    private static decimal CalcularCosto(IReadOnlyCollection<ProductoVariante> variantes)
    {
        if (variantes.Count == 0) return 0m;
        var total = variantes.Sum(v => v.Cantidad);
        if (total > 0)
            return Math.Round(variantes.Sum(v => (v.Costo ?? 0m) * v.Cantidad) / total, 2, MidpointRounding.AwayFromZero);
        var costos = variantes.Where(v => v.Costo.HasValue).Select(v => v.Costo!.Value).ToList();
        return costos.Count > 0 ? Math.Round(costos.Average(), 2, MidpointRounding.AwayFromZero) : 0m;
    }

    private static int? ValorComun(IReadOnlyCollection<ProductoVariante> variantes, Func<ProductoVariante, int?> selector)
    {
        if (variantes.Count == 0) return null;
        var valores = variantes.Select(selector).Distinct().Take(2).ToList();
        return valores.Count == 1 ? valores[0] : null;
    }

    private static bool NoVacio(string? valor) => !string.IsNullOrWhiteSpace(valor);

    private static string ConstruirEtiqueta(ProductoVariante variante)
    {
        var partes = new[] { variante.Marca?.Nombre, variante.Modelo?.Nombre, variante.Color?.Nombre, variante.Talla?.Nombre, variante.Sku };
        return string.Join(" · ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
    }
}
