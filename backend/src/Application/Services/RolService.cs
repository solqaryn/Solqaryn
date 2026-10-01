using System.Globalization;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class RolService : IRolService
{
    private readonly IRolRepository _repository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;

    public RolService(IRolRepository repository, IAuditoriaService auditoria, ICurrentUserService currentUser)
    {
        _repository = repository;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    private static string Normalizar(string nombre) =>
        nombre.Trim().ToLower(CultureInfo.InvariantCulture);

    public async Task<List<RolDto>> GetAllAsync(bool incluirEliminados = false)
    {
        var roles = await _repository.GetAllAsync(incluirEliminados);
        var resultado = new List<RolDto>();
        foreach (var rol in roles)
            resultado.Add(await MapAsync(rol));
        return resultado;
    }

    public async Task<RolDto?> GetByIdAsync(int id)
    {
        var rol = await _repository.GetByIdAsync(id);
        return rol is null ? null : await MapAsync(rol);
    }

    private async Task<RolDto> MapAsync(Rol rol) => new()
    {
        Id = rol.Id,
        Nombre = rol.Nombre,
        Descripcion = rol.Descripcion,
        EsSistema = rol.EsSistema,
        EsAdministrador = rol.EsAdministrador,
        Activo = rol.Activo,
        CantidadUsuarios = await _repository.ContarUsuariosAsync(rol.Id),
        CantidadPermisos = await _repository.ContarPermisosAsync(rol.Id),
        FechaCreacion = rol.FechaCreacion,
        FechaActualizacion = rol.FechaActualizacion
    };

    public async Task<RolDto> CreateAsync(CrearRolDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new BusinessRuleException("El nombre del rol es obligatorio.");

        var normalizado = Normalizar(nombre);
        if (await _repository.ExisteNombreAsync(normalizado))
            throw new BusinessRuleException($"Ya existe un rol con el nombre '{nombre}'.");

        var rol = new Rol
        {
            Nombre = nombre,
            NombreNormalizado = normalizado,
            Descripcion = dto.Descripcion,
            EsAdministrador = dto.EsAdministrador,
            EsSistema = false,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Crear,
            $"Creó el rol '{rol.Nombre}'.", rol.Id);

        return await MapAsync(rol);
    }

    public async Task<RolDto> UpdateAsync(int id, ActualizarRolDto dto)
    {
        var rol = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El rol no existe.");

        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new BusinessRuleException("El nombre del rol es obligatorio.");

        var normalizado = Normalizar(nombre);
        if (await _repository.ExisteNombreAsync(normalizado, id))
            throw new BusinessRuleException($"Ya existe un rol con el nombre '{nombre}'.");

        if (rol.EsSistema && normalizado != rol.NombreNormalizado)
            throw new BusinessRuleException("No se puede renombrar un rol de sistema.");

        var anterior = new { rol.Nombre, rol.Descripcion };

        rol.Nombre = nombre;
        rol.NombreNormalizado = normalizado;
        rol.Descripcion = dto.Descripcion;
        rol.FechaActualizacion = DateTime.UtcNow;
        rol.ActualizadoPorUsuarioId = _currentUser.UsuarioId;

        _repository.Update(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Editar,
            $"Editó el rol '{rol.Nombre}'.", rol.Id,
            entidad: "Rol", valoresAnteriores: anterior, valoresNuevos: new { rol.Nombre, rol.Descripcion });

        return await MapAsync(rol);
    }

    public async Task<RolDto> ActivarAsync(int id)
    {
        var rol = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El rol no existe.");

        rol.Activo = true;
        rol.FechaActualizacion = DateTime.UtcNow;
        rol.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Activar,
            $"Activó el rol '{rol.Nombre}'.", rol.Id);

        return await MapAsync(rol);
    }

    public async Task<RolDto> DesactivarAsync(int id)
    {
        var rol = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El rol no existe.");

        if (rol.EsAdministrador)
        {
            var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirRolId: rol.Id);
            if (otrosAdmins == 0)
                throw new BusinessRuleException(
                    "No se puede desactivar el último rol administrador con usuarios activos.");
        }

        rol.Activo = false;
        rol.FechaActualizacion = DateTime.UtcNow;
        rol.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Desactivar,
            $"Desactivó el rol '{rol.Nombre}'.", rol.Id);

        return await MapAsync(rol);
    }

    public async Task EliminarLogicoAsync(int id)
    {
        var rol = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El rol no existe.");

        if (rol.EsSistema)
            throw new BusinessRuleException("No se puede eliminar un rol de sistema.");

        var usuarios = await _repository.ContarUsuariosAsync(rol.Id);
        if (usuarios > 0)
            throw new BusinessRuleException("No se puede eliminar un rol que tiene usuarios asignados.");

        if (rol.EsAdministrador)
        {
            var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirRolId: rol.Id);
            if (otrosAdmins == 0)
                throw new BusinessRuleException("No se puede eliminar el último rol administrador.");
        }

        rol.Eliminado = true;
        rol.Activo = false;
        rol.FechaEliminacion = DateTime.UtcNow;
        rol.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.EliminarLogico,
            $"Eliminó (lógico) el rol '{rol.Nombre}'.", rol.Id);
    }

    public async Task EliminarPermanenteAsync(int id)
    {
        var rol = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El rol no existe.");

        if (rol.EsSistema)
            throw new BusinessRuleException("No se puede eliminar permanentemente un rol de sistema.");

        var usuarios = await _repository.ContarUsuariosAsync(rol.Id);
        if (usuarios > 0)
            throw new BusinessRuleException("No se puede eliminar permanentemente un rol con usuarios asociados.");

        var permisos = await _repository.ContarPermisosAsync(rol.Id);
        if (permisos > 0)
            throw new BusinessRuleException("No se puede eliminar permanentemente un rol con permisos asignados.");

        // Hueco cerrado (sección 7 del prompt: "no debe eliminarse el último
        // rol con administración completa"): esta validación ya existía en
        // Desactivar y EliminarLogico pero faltaba aquí. Un rol EsAdministrador
        // sin usuarios asignados actualmente igual puede ser el ÚNICO rol
        // capaz de tener administradores en el sistema — eliminarlo
        // permanentemente dejaría al sistema sin ninguna vía de crear un
        // nuevo administrador sin intervención directa en base de datos.
        if (rol.EsAdministrador)
        {
            var otrosAdminsRoles = await _repository.ContarRolesAdministradorAsync(excluirRolId: rol.Id);
            if (otrosAdminsRoles == 0)
                throw new BusinessRuleException("No se puede eliminar permanentemente el último rol de tipo administrador del sistema.");
        }

        var nombre = rol.Nombre;
        _repository.Remove(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.EliminarPermanente,
            $"Eliminó permanentemente el rol '{nombre}'.", id);
    }

    public async Task<RolDto> DuplicarAsync(int id, string nuevoNombre)
    {
        var original = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El rol no existe.");

        var nombre = nuevoNombre.Trim();
        var normalizado = Normalizar(nombre);
        if (await _repository.ExisteNombreAsync(normalizado))
            throw new BusinessRuleException($"Ya existe un rol con el nombre '{nombre}'.");

        var copia = new Rol
        {
            Nombre = nombre,
            NombreNormalizado = normalizado,
            Descripcion = original.Descripcion,
            EsAdministrador = original.EsAdministrador,
            EsSistema = false,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(copia);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Duplicar,
            $"Duplicó el rol '{original.Nombre}' como '{copia.Nombre}'.", copia.Id);

        return await MapAsync(copia);
    }
}
