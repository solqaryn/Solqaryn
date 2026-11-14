namespace Solqaryn.Application.DTOs;

public sealed class ResultadoEnvioCorreoDto
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsTransitorio { get; set; }
    public bool YaProcesado { get; set; }
    public int Intentos { get; set; }
    public string? MessageId { get; set; }
}
