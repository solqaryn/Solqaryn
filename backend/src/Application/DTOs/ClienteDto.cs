namespace Solqaryn.Application.DTOs;

public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? IdentidadORTN { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public int TotalVentas { get; set; }
    public decimal TotalVendido { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int TipoClienteId { get; set; }
    public string TipoClienteNombre { get; set; } = string.Empty;
    public string TipoClienteColorHex { get; set; } = string.Empty;
}

public class CreateClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? IdentidadORTN { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public int? TipoClienteId { get; set; }
}

public class UpdateClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? IdentidadORTN { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public int? TipoClienteId { get; set; }
}
