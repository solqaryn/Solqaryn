using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class TransferenciaInventarioDetalleInputDto
{
    public int ProductoVarianteId { get; set; }
    public int? UbicacionOrigenId { get; set; }
    public int? UbicacionDestinoId { get; set; }
    public int CantidadSolicitada { get; set; }
}

public sealed class CreateTransferenciaInventarioDto
{
    public int AlmacenOrigenId { get; set; }
    public int AlmacenDestinoId { get; set; }
    public string? Observaciones { get; set; }
    public List<TransferenciaInventarioDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class UpdateTransferenciaInventarioDto
{
    public int AlmacenOrigenId { get; set; }
    public int AlmacenDestinoId { get; set; }
    public string? Observaciones { get; set; }
    public List<TransferenciaInventarioDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class AprobarTransferenciaInventarioDetalleDto
{
    public int DetalleId { get; set; }
    public int CantidadAprobada { get; set; }
}

public sealed class AprobarTransferenciaInventarioDto
{
    public List<AprobarTransferenciaInventarioDetalleDto> Detalles { get; set; } = new();
}

public sealed class DespacharTransferenciaInventarioDetalleDto
{
    public int DetalleId { get; set; }
    public int CantidadDespachada { get; set; }
}

public sealed class DespacharTransferenciaInventarioDto
{
    public List<DespacharTransferenciaInventarioDetalleDto> Detalles { get; set; } = new();
}

public sealed class RecibirTransferenciaInventarioDetalleDto
{
    public int DetalleId { get; set; }
    public int CantidadRecibida { get; set; }
    public int CantidadFaltante { get; set; }
    public int CantidadDanada { get; set; }
    public int CantidadSobrante { get; set; }
}

public sealed class RecibirTransferenciaInventarioDto
{
    public List<RecibirTransferenciaInventarioDetalleDto> Detalles { get; set; } = new();
}

public sealed class CancelarTransferenciaInventarioDto
{
    public string Motivo { get; set; } = string.Empty;
}

public sealed class TransferenciaInventarioFiltroDto : PagedRequest
{
    public TransferenciaInventarioFiltroDto()
    {
        SortBy = "FechaCreacion";
        SortDirection = "desc";
    }

    public EstadoTransferenciaInventario? Estado { get; set; }
    public int? AlmacenOrigenId { get; set; }
    public int? AlmacenDestinoId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Numero { get; set; }
}

public sealed class TransferenciaInventarioDetalleDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int? UbicacionOrigenId { get; set; }
    public int? UbicacionDestinoId { get; set; }
    public int CantidadSolicitada { get; set; }
    public int CantidadAprobada { get; set; }
    public int CantidadDespachada { get; set; }
    public int CantidadRecibida { get; set; }
    public int CantidadFaltante { get; set; }
    public int CantidadSobrante { get; set; }
    public int CantidadDanada { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
}

public sealed class TransferenciaInventarioDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int AlmacenOrigenId { get; set; }
    public string? AlmacenOrigenNombre { get; set; }
    public int AlmacenDestinoId { get; set; }
    public string? AlmacenDestinoNombre { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateTime? FechaSolicitud { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public DateTime? FechaDespacho { get; set; }
    public DateTime? FechaRecepcion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public string? MotivoCancelacion { get; set; }
    public List<TransferenciaInventarioDetalleDto> Detalles { get; set; } = new();
}
