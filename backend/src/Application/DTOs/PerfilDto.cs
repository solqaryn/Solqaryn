using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.DTOs;

public class PerfilDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? FotoPerfilUrl { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class ActualizarPerfilDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}

public class ActualizarFotoPerfilDto
{
    public IFormFile Foto { get; set; } = default!;
}

public class CambiarPasswordPropiaDto
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}
