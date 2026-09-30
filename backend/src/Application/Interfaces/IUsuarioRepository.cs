using Solqaryn.Application.Common;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);
    Task<Usuario?> GetByIdAsync(int id);
    Task<List<Usuario>> GetAllAsync();
    Task<PagedResult<Usuario>> GetPagedAsync(PagedRequest request);
    Task<int> ContarAdministradoresActivosAsync(int? excluirUsuarioId = null);
    Task AddAsync(Usuario usuario);
    void Update(Usuario usuario);

    Task<List<UsuarioEmpresa>> GetEmpresasAsync(int usuarioId);
    Task<UsuarioEmpresa?> GetEmpresaAsync(int usuarioId, int empresaId);
    Task AddEmpresaAsync(UsuarioEmpresa usuarioEmpresa);
    void UpdateEmpresa(UsuarioEmpresa usuarioEmpresa);

    Task<bool> SaveChangesAsync();
}
