using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class ConsumoInsumo : AuditableEntity
{
    public string NumeroConsumo { get; set; } = string.Empty;
    public DateTime FechaConsumo { get; set; } = DateTime.UtcNow;
    public EstadoConsumoInsumo Estado { get; set; } = EstadoConsumoInsumo.Borrador;

    public string AreaDestino { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public DateTime? FechaConfirmacion { get; set; }
    public int? ConfirmadoPorUsuarioId { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }

    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladoPorUsuarioId { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }

    public ICollection<ConsumoInsumoDetalle> Detalles { get; set; } = new List<ConsumoInsumoDetalle>();
}
