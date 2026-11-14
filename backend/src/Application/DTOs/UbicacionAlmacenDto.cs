namespace Solqaryn.Application.DTOs;

public class UbicacionAlmacenDto
{
    public int Id { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenCodigo { get; set; } = string.Empty;
    public string AlmacenNombre { get; set; } = string.Empty;
    public int? UbicacionPadreId { get; set; }
    public string? UbicacionPadreCodigo { get; set; }
    public string? UbicacionPadreNombre { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateUbicacionAlmacenDto
{
    public int AlmacenId { get; set; }
    public int? UbicacionPadreId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}

public class UpdateUbicacionAlmacenDto
{
    public int AlmacenId { get; set; }
    public int? UbicacionPadreId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}

public class TipoUbicacionAlmacenDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
