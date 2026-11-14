namespace Solqaryn.Domain.Entities;

/// <summary>
/// Persisted presentation configuration for one supported dashboard KPI.
/// Exactly one owner must be set: UsuarioId or RolId.
/// </summary>
public sealed class DashboardKpiConfiguracion
{
    public int Id { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }
    public int? RolId { get; set; }
    public bool Habilitado { get; set; } = true;
    public int Orden { get; set; }
    public string? EtiquetaVisible { get; set; }

    public Usuario? Usuario { get; set; }
    public Rol? Rol { get; set; }
}
