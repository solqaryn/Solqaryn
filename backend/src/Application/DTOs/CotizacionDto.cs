using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class CotizacionDto
{
    public int Id { get; set; }
    public EstadoCotizacion Estado { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombreSnapshot { get; set; } = string.Empty;
    public string? ClienteDocumentoSnapshot { get; set; }
    public string? Observaciones { get; set; }
    public decimal Total { get; set; }

    public DateTime? FechaEnvioUtc { get; set; }
    public int? EnviadaPorUsuarioId { get; set; }
    public DateTime? FechaAceptacionUtc { get; set; }
    public int? AceptadaPorUsuarioId { get; set; }
    public DateTime? FechaRechazoUtc { get; set; }
    public int? RechazadaPorUsuarioId { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime? FechaConversionUtc { get; set; }
    public int? ConvertidaPorUsuarioId { get; set; }

    public DateTime CreatedAt { get; set; }
    public int? CreadoPorUsuarioId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }

    public List<CotizacionDetalleDto> Detalles { get; set; } = new();
}

public class CotizacionDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Total { get; set; }
}
