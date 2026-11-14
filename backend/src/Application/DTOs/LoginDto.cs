namespace Solqaryn.Application.DTOs;

public class LoginRequestDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? FotoPerfilUrl { get; set; }
    public DateTime ExpiraEn { get; set; }
}
