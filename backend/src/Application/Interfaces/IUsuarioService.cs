using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IUsuarioService
{
    Task<List<UsuarioDto>> GetAllAsync();
    Task<PagedResult<UsuarioDto>> GetPagedAsync(PagedRequest request);
    Task<UsuarioDetalleDto?> GetByIdAsync(int id);
    // Revalidar el RolId frente al tenant ya autorizado por el gate HTTP.
    Task ValidarRolEmpresaAsync(int? rolId, int empresaIdAutorizada);
    // Administracion empresarial: no modificar campos globales de A+B.
    Task<UsuarioDto?> UpdateDesdeEmpresaAsync(int id, UpdateUsuarioDto dto, int empresaIdAutorizada);
    Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto);
    Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto);
    Task<UsuarioDto?> UpdateEstadoAsync(int id, bool activo);
    Task<UsuarioDto> BloquearAsync(int id, string motivo);
    Task<UsuarioDto> DesbloquearAsync(int id);
    Task EliminarAsync(int id);

    Task<List<UsuarioEmpresaDto>> GetEmpresasAsync(int usuarioId);
    Task<UsuarioEmpresaDto> AsignarEmpresaAsync(int usuarioId, AsignarUsuarioEmpresaDto dto);
    Task<UsuarioEmpresaDto> CambiarRolEmpresaAsync(int usuarioId, int empresaId, int rolId);
    Task<UsuarioEmpresaDto> CambiarEstadoEmpresaAsync(int usuarioId, int empresaId, bool activa);
}
