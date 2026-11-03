using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Mutaciones de identidades por administradores del tenant. La empresa procede
/// exclusivamente del gate RBAC de la API; no del cuerpo de la solicitud.
/// </summary>
public interface IUsuarioAdministracionEmpresaService
{
    Task<UsuarioDto> CrearAsync(
        CreateUsuarioDto dto, int empresaIdAutorizada, CancellationToken ct = default);
    Task<UsuarioDto?> ActualizarAsync(
        int usuarioId, UpdateUsuarioDto dto, int empresaIdAutorizada,
        CancellationToken ct = default);
}
