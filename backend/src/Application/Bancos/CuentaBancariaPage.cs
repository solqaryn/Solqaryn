namespace Solqaryn.Application.Bancos;

/// <summary>
/// Contrato de resultado paginado para cuentas bancarias.
/// </summary>
public sealed class CuentaBancariaPage<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public CuentaBancariaPage(IEnumerable<T>? items, int page, int pageSize, int totalCount)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "La página debe ser mayor o igual a 1.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "El tamaño de página debe ser mayor o igual a 1.");
        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount), "El total de elementos no puede ser negativo.");

        Items = (items ?? Enumerable.Empty<T>()).ToList();
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
