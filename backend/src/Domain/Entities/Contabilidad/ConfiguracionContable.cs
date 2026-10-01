using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities.Contabilidad;

/// <summary>
/// Configuración persistida del motor de contabilización por evento de negocio.
/// </summary>
public class ConfiguracionContable : AuditableEntity
{
    public TipoEventoContable Evento { get; set; }
    public int CuentaDebeId { get; set; }
    public int CuentaHaberId { get; set; }
    public CuentaContable CuentaDebe { get; set; } = null!;
    public CuentaContable CuentaHaber { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public string? Descripcion { get; set; }
}
