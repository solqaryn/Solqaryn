namespace Solqaryn.Application.DTOs;

public class SucursalDto
{
    public int Id { get; set; }
    public int? EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;
    public bool Activa { get; set; }

    public string? CreadoPorNombreUsuario { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateSucursalDto
{
    public int? EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
}

public class UpdateSucursalDto
{
    public int? EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
}
