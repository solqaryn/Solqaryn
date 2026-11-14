namespace Solqaryn.Application.DTOs;

public class ProductoImagenDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool EsPrincipal { get; set; }
    public int? ProductoVarianteId { get; set; }
}
