using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class PermisoCatalogoService : IPermisoCatalogoService
{
    private readonly IPermisoRepository _repository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;

    public PermisoCatalogoService(IPermisoRepository repository, IAuditoriaService auditoria, ICurrentUserService currentUser)
    {
        _repository = repository;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    private static string Codigo(ModuloSistema modulo, AccionPermiso accion) =>
        $"{modulo.ToString().ToUpperInvariant()}.{accion.ToString().ToUpperInvariant()}";

    public async Task<List<PermisoCatalogoDto>> GetAllAsync(bool incluirEliminados = false)
    {
        var permisos = await _repository.GetAllAsync(incluirEliminados);
        var resultado = new List<PermisoCatalogoDto>();
        foreach (var p in permisos)
            resultado.Add(await MapAsync(p));
        return resultado;
    }

    public async Task<PermisoCatalogoDto?> GetByIdAsync(int id)
    {
        var permiso = await _repository.GetByIdAsync(id);
        return permiso is null ? null : await MapAsync(permiso);
    }

    private async Task<PermisoCatalogoDto> MapAsync(Permiso p) => new()
    {
        Id = p.Id,
        Codigo = p.Codigo,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Modulo = p.Modulo.ToString(),
        Accion = p.Accion.ToString(),
        EsSistema = p.EsSistema,
        Activo = p.Activo,
        CantidadRolesAsignados = await _repository.ContarAsignacionesAsync(p.Id),
        FechaCreacion = p.FechaCreacion,
        FechaActualizacion = p.FechaActualizacion
    };

    private static (ModuloSistema, AccionPermiso) ParseOrThrow(string modulo, string accion)
    {
        if (!Enum.TryParse<ModuloSistema>(modulo, true, out var m))
            throw new BusinessRuleException($"Módulo '{modulo}' no es válido.");
        if (!Enum.TryParse<AccionPermiso>(accion, true, out var a))
            throw new BusinessRuleException($"Acción '{accion}' no es válida.");

        var permitido = CatalogoPermisosBase.Definicion
            .Any(d => d.Modulo == m && d.Acciones.Contains(a));
        if (!permitido)
            throw new BusinessRuleException($"La acción '{a}' no aplica al módulo '{m}'.");

        return (m, a);
    }

    public async Task<PermisoCatalogoDto> CreateAsync(CrearPermisoDto dto)
    {
        var (modulo, accion) = ParseOrThrow(dto.Modulo, dto.Accion);

        if (await _repository.ExisteModuloAccionAsync(modulo, accion))
            throw new BusinessRuleException($"Ya existe un permiso para '{modulo}.{accion}'.");

        var codigo = Codigo(modulo, accion);
        if (await _repository.ExisteCodigoAsync(codigo))
            throw new BusinessRuleException($"Ya existe un permiso con código '{codigo}'.");

        var permiso = new Permiso
        {
            Codigo = codigo,
            Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? codigo : dto.Nombre.Trim(),
            Descripcion = dto.Descripcion,
            Modulo = modulo,
            Accion = accion,
            EsSistema = false,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(permiso);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Permisos, AccionPermiso.Crear,
            $"Creó el permiso '{permiso.Codigo}'.", permiso.Id);

        return await MapAsync(permiso);
    }

    public async Task<PermisoCatalogoDto> UpdateAsync(int id, ActualizarPermisoDto dto)
    {
        var permiso = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El permiso no existe.");

        if (permiso.EsSistema)
            throw new BusinessRuleException("No se puede modificar un permiso de sistema.");

        permiso.Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? permiso.Nombre : dto.Nombre.Trim();
        permiso.Descripcion = dto.Descripcion;
        permiso.FechaActualizacion = DateTime.UtcNow;
        permiso.ActualizadoPorUsuarioId = _currentUser.UsuarioId;

        _repository.Update(permiso);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Permisos, AccionPermiso.Editar,
            $"Editó el permiso '{permiso.Codigo}'.", permiso.Id);

        return await MapAsync(permiso);
    }

    public async Task<PermisoCatalogoDto> ActivarAsync(int id)
    {
        var permiso = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El permiso no existe.");

        permiso.Activo = true;
        permiso.FechaActualizacion = DateTime.UtcNow;
        permiso.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(permiso);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Permisos, AccionPermiso.Activar,
            $"Activó el permiso '{permiso.Codigo}'.", permiso.Id);

        return await MapAsync(permiso);
    }

    public async Task<PermisoCatalogoDto> DesactivarAsync(int id)
    {
        var permiso = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El permiso no existe.");

        if (permiso.EsSistema)
            throw new BusinessRuleException("No se puede desactivar un permiso de sistema.");

        permiso.Activo = false;
        permiso.FechaActualizacion = DateTime.UtcNow;
        permiso.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(permiso);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Permisos, AccionPermiso.Desactivar,
            $"Desactivó el permiso '{permiso.Codigo}'.", permiso.Id);

        return await MapAsync(permiso);
    }

    public async Task EliminarLogicoAsync(int id)
    {
        var permiso = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El permiso no existe.");

        if (permiso.EsSistema)
            throw new BusinessRuleException("No se puede eliminar un permiso de sistema.");

        var asignaciones = await _repository.ContarAsignacionesAsync(permiso.Id);
        if (asignaciones > 0)
            throw new BusinessRuleException("No se puede eliminar un permiso asignado a uno o más roles.");

        permiso.Eliminado = true;
        permiso.Activo = false;
        permiso.FechaEliminacion = DateTime.UtcNow;
        permiso.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(permiso);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Permisos, AccionPermiso.EliminarLogico,
            $"Eliminó (lógico) el permiso '{permiso.Codigo}'.", permiso.Id);
    }

    public async Task EliminarPermanenteAsync(int id)
    {
        var permiso = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El permiso no existe.");

        if (permiso.EsSistema)
            throw new BusinessRuleException("No se puede eliminar permanentemente un permiso de sistema.");

        var asignaciones = await _repository.ContarAsignacionesAsync(permiso.Id);
        if (asignaciones > 0)
            throw new BusinessRuleException("No se puede eliminar permanentemente un permiso asignado.");

        var codigo = permiso.Codigo;
        _repository.Remove(permiso);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Permisos, AccionPermiso.EliminarPermanente,
            $"Eliminó permanentemente el permiso '{codigo}'.", id);
    }

    public async Task<PermisoCatalogoDto> DuplicarAsync(int id, string nuevoNombre, string nuevaAccion)
    {
        var original = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El permiso no existe.");

        var (modulo, accion) = ParseOrThrow(original.Modulo.ToString(), nuevaAccion);

        if (await _repository.ExisteModuloAccionAsync(modulo, accion))
            throw new BusinessRuleException($"Ya existe un permiso para '{modulo}.{accion}'.");

        var codigo = Codigo(modulo, accion);
        var copia = new Permiso
        {
            Codigo = codigo,
            Nombre = string.IsNullOrWhiteSpace(nuevoNombre) ? codigo : nuevoNombre.Trim(),
            Descripcion = original.Descripcion,
            Modulo = modulo,
            Accion = accion,
            EsSistema = false,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(copia);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Permisos, AccionPermiso.Duplicar,
            $"Duplicó el permiso '{original.Codigo}' como '{copia.Codigo}'.", copia.Id);

        return await MapAsync(copia);
    }

    public async Task SembrarCatalogoAsync()
    {
        var existentes = await _repository.GetAllAsync(incluirEliminados: true);

        foreach (var (modulo, acciones) in CatalogoPermisosBase.Definicion)
        {
            foreach (var accion in acciones)
            {
                var yaExiste = existentes.Any(p => p.Modulo == modulo && p.Accion == accion);
                if (yaExiste) continue;

                var codigo = Codigo(modulo, accion);
                await _repository.AddAsync(new Permiso
                {
                    Codigo = codigo,
                    Nombre = $"{modulo}: {accion}",
                    Modulo = modulo,
                    Accion = accion,
                    EsSistema = true,
                    Activo = true
                });
            }
        }

        await _repository.SaveChangesAsync();
    }
}
