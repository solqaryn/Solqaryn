namespace Solqaryn.Application.DTOs;

public sealed class InventarioPublicoVarianteDto
{
    public int ProductoVarianteId { get; init; }
    public int CantidadDisponible { get; init; }
    public int StockMinimo { get; init; }
    public bool TieneStockBajo { get; init; }
    public bool EstaAgotada { get; init; }
    public bool TieneFuenteAutoritativa { get; init; }
}
