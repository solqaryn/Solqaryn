using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

/// <summary>Administración empresarial; el tenant procede del gate RBAC, nunca del DTO.</summary>
public interface IUsuarioAdministracionEmpresaService
{
    Task<List<UsuarioDto>> GetAllAsync(int empresaId, CancellationToken ct = default);
    Task<PagedResult<UsuarioDto>> GetPagedAsync(int empresaId, PagedRequest request, CancellationToken ct = default);
    Task<UsuarioDetalleDto?> GetByIdAsync(int empresaId, int id, CancellationToken ct = default);
    Task<UsuarioDto> CrearAsync(int empresaId, CreateUsuarioDto dto, CancellationToken ct = default);
    Task<UsuarioDto?> ActualizarAsync(int empresaId, int id, UpdateUsuarioDto dto, CancellationToken ct = default);
    Task<UsuarioDto> UpdateEstadoAsync(int empresaId, int id, bool activo, CancellationToken ct = default);
    Task<UsuarioDto> BloquearAsync(int empresaId, int id, string motivo, CancellationToken ct = default);
    Task<UsuarioDto> DesbloquearAsync(int empresaId, int id, CancellationToken ct = default);
    Task EliminarAsync(int empresaId, int id, CancellationToken ct = default);
    Task<List<UsuarioEmpresaDto>> GetEmpresasAsync(int empresaId, int id, CancellationToken ct = default);
    Task<UsuarioEmpresaDto> AsignarEmpresaAsync(int empresaId, int id, int rolId, CancellationToken ct = default);
    Task<UsuarioEmpresaDto> CambiarRolEmpresaAsync(int empresaId, int id, int rolId, CancellationToken ct = default);
    Task<UsuarioEmpresaDto> CambiarEstadoEmpresaAsync(int empresaId, int id, bool activa, CancellationToken ct = default);
}
