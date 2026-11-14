namespace Solqaryn.Application.DTOs;

public sealed class InventarioVariantesReporteDto
{
    public int TotalVariantes { get; set; }
    public int TotalUnidades { get; set; }
    public decimal ValorCosto { get; set; }
    public decimal ValorVenta { get; set; }
    public List<InventarioDimensionResumenDto> PorProducto { get; set; } = new();
    public List<InventarioDimensionResumenDto> PorMarca { get; set; } = new();
    public List<InventarioDimensionResumenDto> PorModelo { get; set; } = new();
    public List<InventarioDimensionResumenDto> PorColor { get; set; } = new();
    public List<InventarioDimensionResumenDto> PorTalla { get; set; } = new();
    public List<InventarioVarianteFilaDto> Variantes { get; set; } = new();
}

public sealed class InventarioDimensionResumenDto
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public int Variantes { get; set; }
    public int Unidades { get; set; }
    public decimal ValorCosto { get; set; }
    public decimal ValorVenta { get; set; }
}

public sealed class InventarioVarianteFilaDto
{
    public int ProductoVarianteId { get; set; }
    public int ProductoId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public int? MarcaId { get; set; }
    public string? Marca { get; set; }
    public int? ModeloId { get; set; }
    public string? Modelo { get; set; }
    public int? ColorId { get; set; }
    public string? Color { get; set; }
    public int? TallaId { get; set; }
    public string? Talla { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public decimal ValorCosto { get; set; }
    public decimal ValorVenta { get; set; }
    public bool Activo { get; set; }
    public bool EsTecnica { get; set; }
}
