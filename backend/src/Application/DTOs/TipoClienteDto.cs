namespace Solqaryn.Application.DTOs;

public class TipoClienteDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public bool EsSistema { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF";
    public bool Activo { get; set; }
    public int Orden { get; set; }
    public bool EsPredeterminado { get; set; }
    public int TotalClientesAsignados { get; set; }
}

public class CreateTipoClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF";
    public bool Activo { get; set; } = true;
    public int Orden { get; set; }
    public bool EsPredeterminado { get; set; }
}

public class UpdateTipoClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF";
    public bool Activo { get; set; } = true;
    public int Orden { get; set; }
    public bool EsPredeterminado { get; set; }
}
