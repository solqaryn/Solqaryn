namespace Solqaryn.Application.DTOs;

public class ProveedorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Documento { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public int TotalCompras { get; set; }
    public decimal TotalComprado { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CreateProveedorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Documento { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
}

public class UpdateProveedorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Documento { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
}
