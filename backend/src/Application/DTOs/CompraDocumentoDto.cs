namespace Solqaryn.Application.DTOs;

public class CompraDocumentoDto
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool EsImagen { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
}
