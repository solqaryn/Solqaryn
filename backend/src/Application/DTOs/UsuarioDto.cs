namespace Solqaryn.Application.DTOs;

public class UsuarioDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class UsuarioDetalleDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public string? RolNombre { get; set; }
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }
    public string? MotivoBloqueo { get; set; }
    public DateTime? FechaBloqueo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public class BloquearUsuarioDto
{
    public string Motivo { get; set; } = string.Empty;
}

public class CreateUsuarioDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = "Vendedor";
    public int? RolId { get; set; }
}

public class UpdateUsuarioEstadoDto
{
    public bool Activo { get; set; }
}

public class UpdateUsuarioDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = "Vendedor";
    public int? RolId { get; set; }

    /// Si se informa, se restablece la contraseña del usuario. El controlador
    /// exige además el permiso Usuarios:RestablecerContrasena.
    public string? NuevaPassword { get; set; }
}

/// <summary>
/// Membresía efectiva de un usuario dentro de una empresa/tenant.
/// N6.4.D expone este contrato sin sustituir todavía el contexto de autenticación.
/// </summary>
public class UsuarioEmpresaDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int EmpresaId { get; set; }
    public int RolId { get; set; }
    public bool Activa { get; set; }
}

public class AsignarUsuarioEmpresaDto
{
    public int EmpresaId { get; set; }
    public int RolId { get; set; }
}

public class CambiarRolUsuarioEmpresaDto
{
    public int RolId { get; set; }
}

public class UpdateUsuarioEmpresaEstadoDto
{
    public bool Activa { get; set; }
}
