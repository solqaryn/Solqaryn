using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Autoridad de stock vivo para una variante en un Almacén y, opcionalmente,
/// una Ubicación interna. ERP-N1.4 no duplica SucursalId/EmpresaId: ambos se
/// derivan transitivamente desde Almacen.
/// </summary>
public class ExistenciaVariante : AuditableEntity
{
    public int ProductoVarianteId { get; set; }
    public ProductoVariante ProductoVariante { get; set; } = null!;

    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;

    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public int StockFisico { get; private set; }
    public int StockReservado { get; private set; }
    public int StockDisponible { get; private set; }
    public int StockTransito { get; private set; }
    public int StockMinimo { get; private set; }
    public int? StockMaximo { get; private set; }

    /// <summary>
    /// Define un estado de stock válido. StockDisponible nunca es una entrada
    /// independiente: se deriva siempre de físico - reservado y en persistencia
    /// será reforzado mediante columna generada por MySQL.
    /// </summary>
    public void EstablecerStocks(
        int stockFisico,
        int stockReservado,
        int stockTransito,
        int stockMinimo,
        int? stockMaximo)
    {
        if (stockFisico < 0)
            throw new ArgumentOutOfRangeException(nameof(stockFisico), "El stock físico no puede ser negativo.");
        if (stockReservado < 0)
            throw new ArgumentOutOfRangeException(nameof(stockReservado), "El stock reservado no puede ser negativo.");
        if (stockReservado > stockFisico)
            throw new ArgumentException("El stock reservado no puede superar el stock físico.", nameof(stockReservado));
        if (stockTransito < 0)
            throw new ArgumentOutOfRangeException(nameof(stockTransito), "El stock en tránsito no puede ser negativo.");
        if (stockMinimo < 0)
            throw new ArgumentOutOfRangeException(nameof(stockMinimo), "El stock mínimo no puede ser negativo.");
        if (stockMaximo.HasValue && stockMaximo.Value < stockMinimo)
            throw new ArgumentException("El stock máximo no puede ser menor que el stock mínimo.", nameof(stockMaximo));

        StockFisico = stockFisico;
        StockReservado = stockReservado;
        StockDisponible = stockFisico - stockReservado;
        StockTransito = stockTransito;
        StockMinimo = stockMinimo;
        StockMaximo = stockMaximo;
    }

    public bool TieneStockBajo => StockDisponible <= StockMinimo;
    public bool EstaAgotada => StockDisponible <= 0;
}
