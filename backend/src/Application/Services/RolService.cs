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

    private static void ExigirEmpresa(int empresaId)
    {
        if (empresaId <= 0) throw new ForbiddenAccessException("Se requiere una empresa autorizada.");
    }

    private async Task<Rol> ExigirRolAsync(int id, int? empresaId, bool modificar)
    {
        if (empresaId.HasValue) ExigirEmpresa(empresaId.Value);
        var rol = empresaId.HasValue
            ? await _repository.GetByIdEmpresaAsync(id, empresaId.Value)
            : await _repository.GetByIdAsync(id);
        if (rol is null) throw new BusinessRuleException("El rol no existe.");
        if (empresaId.HasValue && (rol.Ambito != AmbitoAutorizacion.Empresa ||
            (rol.EmpresaId != empresaId && !(rol.EmpresaId == null && rol.EsSistema)) ||
            (modificar && (rol.EsSistema || rol.EmpresaId != empresaId))))
            throw new ForbiddenAccessException("El rol no pertenece a la administración de la empresa autorizada.");
        return rol;
    }

    public async Task<List<RolDto>> GetAllEmpresaAsync(int empresaId, bool incluirEliminados = false)
    {
        ExigirEmpresa(empresaId);
        var roles = await _repository.GetAllEmpresaAsync(empresaId, incluirEliminados);
        var resultado = new List<RolDto>();
        foreach (var rol in roles)
            resultado.Add(await MapAsync(rol, empresaId));
        return resultado;
    }

    public async Task<RolDto?> GetByIdEmpresaAsync(int id, int empresaId)
    {
        ExigirEmpresa(empresaId);
        var rol = await _repository.GetByIdEmpresaAsync(id, empresaId);
        return rol is null ? null : await MapAsync(rol, empresaId);
    }

    private async Task<RolDto> MapAsync(Rol rol, int? empresaId = null) => new()
    {
        Id = rol.Id,
        Nombre = rol.Nombre,
        Descripcion = rol.Descripcion,
        EsSistema = rol.EsSistema,
        EsAdministrador = rol.EsAdministrador,
        Activo = rol.Activo,
        CantidadUsuarios = empresaId.HasValue
            ? await _repository.ContarMembresiasEmpresaAsync(rol.Id, empresaId.Value)
            : await _repository.ContarUsuariosAsync(rol.Id),
        CantidadPermisos = await _repository.ContarPermisosAsync(rol.Id),
        FechaCreacion = rol.FechaCreacion,
        FechaActualizacion = rol.FechaActualizacion
    };

    public Task<RolDto> CreateAsync(CrearRolDto dto) => CreateCoreAsync(dto, null);

    public Task<RolDto> CreateEmpresaAsync(CrearRolDto dto, int empresaId)
    {
        ExigirEmpresa(empresaId);
        return CreateCoreAsync(dto, empresaId);
    }

    private async Task<RolDto> CreateCoreAsync(CrearRolDto dto, int? empresaId)
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
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = empresaId,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Crear,
            $"Creó el rol '{rol.Nombre}'.", rol.Id);

        return await MapAsync(rol, empresaId);
    }

    public Task<RolDto> UpdateAsync(int id, ActualizarRolDto dto) => UpdateCoreAsync(id, dto, null);

    public Task<RolDto> UpdateEmpresaAsync(int id, ActualizarRolDto dto, int empresaId)
    {
        ExigirEmpresa(empresaId);
        return UpdateCoreAsync(id, dto, empresaId);
    }

    private async Task<RolDto> UpdateCoreAsync(int id, ActualizarRolDto dto, int? empresaId)
    {
        var rol = await ExigirRolAsync(id, empresaId, modificar: true);

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

        return await MapAsync(rol, empresaId);
    }

    public Task<RolDto> ActivarAsync(int id) => ActivarCoreAsync(id, null);

    public Task<RolDto> ActivarEmpresaAsync(int id, int empresaId)
    {
        ExigirEmpresa(empresaId);
        return ActivarCoreAsync(id, empresaId);
    }

    private async Task<RolDto> ActivarCoreAsync(int id, int? empresaId)
    {
        var rol = await ExigirRolAsync(id, empresaId, modificar: true);

        rol.Activo = true;
        rol.FechaActualizacion = DateTime.UtcNow;
        rol.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        _repository.Update(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Activar,
            $"Activó el rol '{rol.Nombre}'.", rol.Id);

        return await MapAsync(rol, empresaId);
    }

    public Task<RolDto> DesactivarAsync(int id) => DesactivarCoreAsync(id, null);

    public Task<RolDto> DesactivarEmpresaAsync(int id, int empresaId)
    {
        ExigirEmpresa(empresaId);
        return DesactivarCoreAsync(id, empresaId);
    }

    private async Task<RolDto> DesactivarCoreAsync(int id, int? empresaId)
    {
        var rol = await ExigirRolAsync(id, empresaId, modificar: true);

        if (rol.EsAdministrador)
        {
            var otrosAdmins = empresaId.HasValue
                ? await _repository.ContarAdministradoresEmpresaActivosAsync(empresaId.Value, rol.Id)
                : await _repository.ContarAdministradoresActivosAsync(excluirRolId: rol.Id);
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

        return await MapAsync(rol, empresaId);
    }

    public Task EliminarLogicoAsync(int id) => EliminarLogicoCoreAsync(id, null);

    public Task EliminarLogicoEmpresaAsync(int id, int empresaId)
    {
        ExigirEmpresa(empresaId);
        return EliminarLogicoCoreAsync(id, empresaId);
    }

    private async Task EliminarLogicoCoreAsync(int id, int? empresaId)
    {
        var rol = await ExigirRolAsync(id, empresaId, modificar: true);

        if (rol.EsSistema)
            throw new BusinessRuleException("No se puede eliminar un rol de sistema.");

        var usuarios = await _repository.ContarUsuariosAsync(rol.Id);
        if (empresaId.HasValue) usuarios += await _repository.ContarMembresiasPorRolAsync(rol.Id);
        if (usuarios > 0)
            throw new BusinessRuleException("No se puede eliminar un rol que tiene usuarios asignados.");

        if (rol.EsAdministrador)
        {
            var otrosAdmins = empresaId.HasValue
                ? await _repository.ContarAdministradoresEmpresaActivosAsync(empresaId.Value, rol.Id)
                : await _repository.ContarAdministradoresActivosAsync(excluirRolId: rol.Id);
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

    public Task EliminarPermanenteAsync(int id) => EliminarPermanenteCoreAsync(id, null);

    public Task EliminarPermanenteEmpresaAsync(int id, int empresaId)
    {
        ExigirEmpresa(empresaId);
        return EliminarPermanenteCoreAsync(id, empresaId);
    }

    private async Task EliminarPermanenteCoreAsync(int id, int? empresaId)
    {
        var rol = await ExigirRolAsync(id, empresaId, modificar: true);

        if (rol.EsSistema)
            throw new BusinessRuleException("No se puede eliminar permanentemente un rol de sistema.");

        var usuarios = await _repository.ContarUsuariosAsync(rol.Id);
        if (empresaId.HasValue) usuarios += await _repository.ContarMembresiasPorRolAsync(rol.Id);
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
            var otrosAdminsRoles = empresaId.HasValue
                ? await _repository.ContarRolesAdministradorEmpresaAsync(empresaId.Value, rol.Id)
                : await _repository.ContarRolesAdministradorAsync(excluirRolId: rol.Id);
            if (otrosAdminsRoles == 0)
                throw new BusinessRuleException("No se puede eliminar permanentemente el último rol de tipo administrador del sistema.");
        }

        var nombre = rol.Nombre;
        _repository.Remove(rol);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.EliminarPermanente,
            $"Eliminó permanentemente el rol '{nombre}'.", id);
    }

    public Task<RolDto> DuplicarAsync(int id, string nuevoNombre) => DuplicarCoreAsync(id, nuevoNombre, null);

    public Task<RolDto> DuplicarEmpresaAsync(int id, string nuevoNombre, int empresaId)
    {
        ExigirEmpresa(empresaId);
        return DuplicarCoreAsync(id, nuevoNombre, empresaId);
    }

    private async Task<RolDto> DuplicarCoreAsync(int id, string nuevoNombre, int? empresaId)
    {
        var original = await ExigirRolAsync(id, empresaId, modificar: false);

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
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = empresaId,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(copia);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Roles, AccionPermiso.Duplicar,
            $"Duplicó el rol '{original.Nombre}' como '{copia.Nombre}'.", copia.Id);

        return await MapAsync(copia, empresaId);
    }
}
