namespace Solqaryn.Domain.Entities;

/// Tema visual global de la aplicación (sección 16 del prompt). Fila única
/// (Id=1) — configuración global administrada solo por usuarios
/// autorizados, no personalización por usuario individual (el prompt
/// permite que coexista una configuración por usuario, pero no existía
/// ninguna en este proyecto, así que no se inventa una).
public class TemaVisual
{
    public int Id { get; set; }

    public string ColorPrimario { get; set; } = "#0284c7";
    public string ColorSecundario { get; set; } = "#075985";
    public string ColorAcento { get; set; } = "#f97316";
    public string FondoPrincipal { get; set; } = "#f3f7fb";
    public string FondoTarjetas { get; set; } = "#ffffff";
    public string MenuLateral { get; set; } = "#08131f";
    public string BarraSuperior { get; set; } = "#ffffff";
    public string Encabezados { get; set; } = "#0f172a";
    public string BotonesPrincipales { get; set; } = "#0284c7";
    public string TextoPrincipal { get; set; } = "#111827";
    public string TextoSecundario { get; set; } = "#6b7280";
    public string ColorExito { get; set; } = "#10b981";
    public string ColorAdvertencia { get; set; } = "#f59e0b";
    public string ColorError { get; set; } = "#ef4444";
    public string ColorInformacion { get; set; } = "#3b82f6";

    public DateTime? FechaActualizacion { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
}
