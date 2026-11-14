using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Common;

public static class ReporteInventarioQueryRules
{
    public const int MaxHistoricalDays = 366;

    public static string? Validate(ReporteInventarioFiltroBaseDto filtro, params string[] allowedSortFields)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var temporal = NormalizeHistoricalWindow(filtro, DateTime.UtcNow, MaxHistoricalDays);
        if (temporal is not null)
            return temporal;

        return ValidateSort(filtro, allowedSortFields);
    }

    public static string? ValidateStockHealth(ReporteInventarioStockHealthFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        if (filtro.Dias is < 1 or > MaxHistoricalDays)
            return $"Dias debe estar entre 1 y {MaxHistoricalDays}.";

        var temporal = NormalizeHistoricalWindow(filtro, DateTime.UtcNow, filtro.Dias);
        if (temporal is not null)
            return temporal;

        return ValidateSort(filtro, "Fecha", "Producto", "Sku", "StockDisponible", "DiasSinMovimiento", "Rotacion");
    }

    private static string? NormalizeHistoricalWindow(
        ReporteInventarioFiltroBaseDto filtro,
        DateTime utcNow,
        int defaultDays)
    {
        var hasta = filtro.Hasta ?? utcNow;
        var desde = filtro.Desde ?? hasta.AddDays(-defaultDays);

        if (desde > hasta)
            return "El rango de fechas es inválido: Desde no puede ser posterior a Hasta.";

        if ((hasta - desde).TotalDays > MaxHistoricalDays)
            return $"La ventana histórica máxima es de {MaxHistoricalDays} días.";

        // Persist effective bounds in the request so repository/service code never
        // turns an omitted endpoint into an unbounded historical query.
        filtro.Desde = desde;
        filtro.Hasta = hasta;
        return null;
    }

    private static string? ValidateSort(ReporteInventarioFiltroBaseDto filtro, params string[] allowedSortFields)
    {
        var direction = (filtro.SortDirection ?? "desc").Trim().ToLowerInvariant();
        if (direction is not ("asc" or "desc"))
            return "SortDirection sólo admite asc o desc.";

        if (allowedSortFields.Length > 0)
        {
            var sortBy = (filtro.SortBy ?? string.Empty).Trim();
            if (!allowedSortFields.Any(x => string.Equals(x, sortBy, StringComparison.OrdinalIgnoreCase)))
                return $"SortBy no permitido para este reporte: {sortBy}.";
        }

        return null;
    }
}
