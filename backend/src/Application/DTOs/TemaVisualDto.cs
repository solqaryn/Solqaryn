namespace Solqaryn.Application.DTOs;

public class TemaVisualDto
{
    public string ColorPrimario { get; set; } = string.Empty;
    public string ColorSecundario { get; set; } = string.Empty;
    public string ColorAcento { get; set; } = string.Empty;
    public string FondoPrincipal { get; set; } = string.Empty;
    public string FondoTarjetas { get; set; } = string.Empty;
    public string MenuLateral { get; set; } = string.Empty;
    public string BarraSuperior { get; set; } = string.Empty;
    public string Encabezados { get; set; } = string.Empty;
    public string BotonesPrincipales { get; set; } = string.Empty;
    public string TextoPrincipal { get; set; } = string.Empty;
    public string TextoSecundario { get; set; } = string.Empty;
    public string ColorExito { get; set; } = string.Empty;
    public string ColorAdvertencia { get; set; } = string.Empty;
    public string ColorError { get; set; } = string.Empty;
    public string ColorInformacion { get; set; } = string.Empty;
    public DateTime? FechaActualizacion { get; set; }
}

/// Mismo shape que TemaVisualDto: se usa tanto para guardar como para
/// "restaurar predeterminado" (el frontend envía los valores default de
/// fábrica y el backend los persiste igual que cualquier otro guardado).
public class ActualizarTemaVisualDto : TemaVisualDto
{
}
