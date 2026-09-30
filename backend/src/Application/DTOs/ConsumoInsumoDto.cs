namespace Solqaryn.Application.DTOs;

public sealed class ConsumoInsumoDetalleInputDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int Cantidad { get; set; }
}

public sealed class CreateConsumoInsumoDto
{
    public DateTime? FechaConsumo { get; set; }
    public string AreaDestino { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<ConsumoInsumoDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class UpdateConsumoInsumoDto
{
    public DateTime? FechaConsumo { get; set; }
    public string AreaDestino { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<ConsumoInsumoDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class AnularConsumoInsumoDto
{
    public string MotivoAnulacion { get; set; } = string.Empty;
}

public sealed class ConsumoInsumoDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int Cantidad { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public decimal CostoTotalSnapshot { get; set; }
    public string NombreSnapshot { get; set; } = string.Empty;
    public string? SkuSnapshot { get; set; }
    public string? ColorSnapshot { get; set; }
}

public sealed class ConsumoInsumoDto
{
    public int Id { get; set; }
    public string NumeroConsumo { get; set; } = string.Empty;
    public DateTime FechaConsumo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string AreaDestino { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
    public decimal CostoTotalSnapshot { get; set; }
    public List<ConsumoInsumoDetalleDto> Detalles { get; set; } = new();
}
