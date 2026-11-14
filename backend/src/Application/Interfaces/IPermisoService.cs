using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IPermisoService
{
    /// Matriz completa del rol indicado (todas las combinaciones Módulo/Acción
    /// válidas según CatalogoPermisosBase, con Permitido según lo guardado).
    Task<List<PermisoMatrizItemDto>> GetMatrizAsync(int rolId);
    Task<List<PermisoMatrizItemDto>> UpdateMatrizAsync(int rolId, UpdatePermisoMatrizDto dto);

    /// Siembra los permisos por defecto de un rol recién creado, solo si el rol
    /// no tiene ninguna fila en su matriz todavía (idempotente, sección 8/9).
    Task PrecargarMatrizPorDefectoAsync(int rolId, bool esAdministrador);

    Task<MisPermisosDto> GetMisPermisosAsync();
    Task<MisPermisosDto> GetMisPermisosAsync(int empresaId);
    Task<bool> TienePermisoAsync(ModuloSistema modulo, AccionPermiso accion);
    Task<bool> TienePermisoAsync(int empresaId, ModuloSistema modulo, AccionPermiso accion);
    Task VerificarPermisoAsync(ModuloSistema modulo, AccionPermiso accion);
    Task VerificarPermisoAsync(int empresaId, ModuloSistema modulo, AccionPermiso accion);
}
