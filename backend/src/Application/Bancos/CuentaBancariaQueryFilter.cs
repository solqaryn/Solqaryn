using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Application.Bancos;

/// <summary>
/// Contrato normalizado para filtros y paginación de cuentas bancarias.
/// </summary>
public sealed class CuentaBancariaQueryFilter
{
    private int _page = 1;
    private int _pageSize = 10;
    private string? _moneda;
    private string? _searchTerm;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 200);
    }

    public int? BancoId { get; set; }
    public EstadoCuentaBancaria? Estado { get; set; }

    public string? Moneda
    {
        get => _moneda;
        set => _moneda = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    public string? SearchTerm
    {
        get => _searchTerm;
        set => _searchTerm = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
