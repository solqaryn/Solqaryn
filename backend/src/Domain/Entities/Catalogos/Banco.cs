using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities.Catalogos;

/// <summary>
/// Catálogo normalizado de instituciones bancarias. Es autoridad relacional
/// reutilizable para pagos y, posteriormente, para Tesorería/Cuentas Bancarias.
/// </summary>
public class Banco : AuditableEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? SwiftBic { get; set; }
    public bool Activo { get; set; } = true;

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    /// <summary>
    /// Autoridad normalizada de unicidad funcional para Codigo.
    /// </summary>
    public string? CodigoNormalizado { get; private set; }
}
