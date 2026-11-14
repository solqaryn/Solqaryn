using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class DevolucionClienteDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int? FacturaId { get; set; }
    public EstadoDevolucionCliente Estado { get; set; }
    public string? Observaciones { get; set; }
    public string? IdempotencyKey { get; set; }
    public decimal MontoReferencia { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
    public List<DevolucionClienteDetalleDto> Detalles { get; set; } = new();
}

public sealed class DevolucionClienteDetalleDto
{
    public int Id { get; set; }
    public int VentaDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string ProductoNombreSnapshot { get; set; } = string.Empty;
    public string ProductoMarcaSnapshot { get; set; } = string.Empty;
    public string ProductoModeloSnapshot { get; set; } = string.Empty;
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
    public int Cantidad { get; set; }
    public int CantidadVendidaSnapshot { get; set; }
    public decimal PrecioUnitarioSnapshot { get; set; }
    public TipoResolucionDevolucionCliente Resolucion { get; set; }
    public decimal MontoReferencia { get; set; }
}

public sealed class CreateDevolucionClienteDto
{
    public int VentaId { get; set; }
    public int? FacturaId { get; set; }
    public string? Observaciones { get; set; }
    public List<CreateDevolucionClienteDetalleDto> Detalles { get; set; } = new();
}

public sealed class CreateDevolucionClienteDetalleDto
{
    public int VentaDetalleId { get; set; }
    public int Cantidad { get; set; }
    public TipoResolucionDevolucionCliente Resolucion { get; set; }
}

public sealed class DevolucionClienteFiltroDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? VentaId { get; set; }
    public EstadoDevolucionCliente? Estado { get; set; }
}

public sealed class AnularDevolucionClienteDto
{
    public string Motivo { get; set; } = string.Empty;
}
