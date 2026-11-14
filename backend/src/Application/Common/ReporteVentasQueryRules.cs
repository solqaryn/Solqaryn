using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Common;

/// <summary>
/// Pure validation and dimension-semantics rules for N5.3 sales reporting.
/// This type does not query the database, authorize users, or infer a sale-level
/// branch that is not present in the persisted model.
/// </summary>
public static class ReporteVentasQueryRules
{
    public const int MaxPageSize = 200;

    public const string SucursalSemantics = "DETAIL_WAREHOUSE_TO_BRANCH";
    public const string CategoriaSemantics = "CURRENT_MASTER_IDENTITY_NO_HISTORICAL_SNAPSHOT";
    public const string HistoricalDisplaySemantics = "VENTA_AND_VENTA_DETALLE_SNAPSHOTS";

    public static IReadOnlyList<string> Validate(ReporteVentasFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var errors = new List<string>();

        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            errors.Add("Desde no puede ser posterior a Hasta.");

        if (filtro.Page < 1)
            errors.Add("Page debe ser mayor o igual a 1.");

        if (filtro.PageSize is < 1 or > MaxPageSize)
            errors.Add($"PageSize debe estar entre 1 y {MaxPageSize}.");

        if (!string.IsNullOrWhiteSpace(filtro.SortDirection)
            && !string.Equals(filtro.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("SortDirection debe ser asc o desc.");
        }

        ValidatePositive(errors, nameof(filtro.VendedorId), filtro.VendedorId);
        ValidatePositive(errors, nameof(filtro.ClienteId), filtro.ClienteId);
        ValidatePositive(errors, nameof(filtro.SucursalId), filtro.SucursalId);
        ValidatePositive(errors, nameof(filtro.CategoriaId), filtro.CategoriaId);
        ValidatePositive(errors, nameof(filtro.ProductoId), filtro.ProductoId);
        ValidatePositive(errors, nameof(filtro.VarianteId), filtro.VarianteId);
        ValidatePositive(errors, nameof(filtro.MarcaId), filtro.MarcaId);
        ValidatePositive(errors, nameof(filtro.ModeloId), filtro.ModeloId);
        ValidatePositive(errors, nameof(filtro.ColorId), filtro.ColorId);
        ValidatePositive(errors, nameof(filtro.TallaId), filtro.TallaId);

        return errors;
    }

    public static bool IsValid(ReporteVentasFiltroDto filtro) => Validate(filtro).Count == 0;

    /// <summary>
    /// Sucursal and product-dimension filters require detail-level semantics.
    /// Sucursal must be resolved through VentaDetalle.AlmacenId -> Almacen.SucursalId;
    /// it must never be inferred from the user or fabricated at Venta header level.
    /// </summary>
    public static bool RequiresDetailDimensionJoin(ReporteVentasFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        return filtro.SucursalId.HasValue
            || filtro.CategoriaId.HasValue
            || filtro.ProductoId.HasValue
            || filtro.VarianteId.HasValue
            || filtro.MarcaId.HasValue
            || filtro.ModeloId.HasValue
            || filtro.ColorId.HasValue
            || filtro.TallaId.HasValue;
    }

    /// <summary>
    /// These filters express current master identity. Historical display values
    /// for product name/brand/model/color/size must still come from durable detail
    /// snapshots when projecting historical rows.
    /// </summary>
    public static bool UsesCurrentMasterIdentityFilter(ReporteVentasFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        return filtro.CategoriaId.HasValue
            || filtro.ProductoId.HasValue
            || filtro.VarianteId.HasValue
            || filtro.MarcaId.HasValue
            || filtro.ModeloId.HasValue
            || filtro.ColorId.HasValue
            || filtro.TallaId.HasValue;
    }

    private static void ValidatePositive(ICollection<string> errors, string field, int? value)
    {
        if (value.HasValue && value.Value <= 0)
            errors.Add($"{field} debe ser mayor que 0 cuando se especifica.");
    }
}
