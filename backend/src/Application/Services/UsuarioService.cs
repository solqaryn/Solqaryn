using System.Text.RegularExpressions;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class UsuarioService : IUsuarioService
{
    private static readonly Regex NombreUsuarioValido = new(
        "^[a-zA-Z0-9._-]{3,50}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IUsuarioRepository _repository;
    private readonly IRolRepository _rolRepository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;

    public UsuarioService(
        IUsuarioRepository repository,
        IRolRepository rolRepository,
        IEmpresaRepository empresaRepository,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _rolRepository = rolRepository;
        _empresaRepository = empresaRepository;
        _auditoria = auditoria;
        _currentUser = currentUser;
    }

    public async Task<List<UsuarioDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<PagedResult<UsuarioDto>> GetPagedAsync(PagedRequest request)
    {
        var resultado = await _repository.GetPagedAsync(request);
        return new PagedResult<UsuarioDto>
        {
            Items = resultado.Items.Select(ToDto).ToList(),
            Page = resultado.Page,
            PageSize = resultado.PageSize,
            TotalCount = resultado.TotalCount
        };
    }

    public async Task<UsuarioDetalleDto?> GetByIdAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id);
        return usuario is null ? null : ToDetalleDto(usuario);
    }

    private async Task<Rol> ResolverRolDinamicoAsync(int? rolId)
    {
        if (!rolId.HasValue || rolId.Value <= 0)
            throw new BusinessRuleException("Debes seleccionar un rol válido.");

        var rol = await _rolRepository.GetByIdAsync(rolId.Value)
            ?? throw new BusinessRuleException("El rol seleccionado no existe.");

        if (!rol.Activo || rol.Eliminado)
            throw new BusinessRuleException("No se puede asignar un rol inactivo o eliminado.");

        return rol;
    }

    public async Task<UsuarioDto> CreateAsync(CreateUsuarioDto dto)
    {
        var nombreUsuario = NormalizarNombreUsuario(dto.NombreUsuario);
        var nombreCompleto = NormalizarNombreCompleto(dto.NombreCompleto);
        ValidarPasswordSegura(dto.Password);

        if (await _repository.GetByNombreUsuarioAsync(nombreUsuario) is not null)
            throw new BusinessRuleException("Ya existe un usuario con ese nombre de usuario.");

        var rol = await ResolverRolDinamicoAsync(dto.RolId);
        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true,
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddAsync(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Crear,
            $"Creó el usuario '{usuario.NombreUsuario}' con rol '{rol.Nombre}'.",
            usuario.Id,
            entidad: "Usuario",
            valoresNuevos: new { usuario.NombreUsuario, usuario.NombreCompleto, RolId = rol.Id, Rol = rol.Nombre });

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateAsync(int id, UpdateUsuarioDto dto)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;

        var nombreUsuario = NormalizarNombreUsuario(dto.NombreUsuario);
        var nombreCompleto = NormalizarNombreCompleto(dto.NombreCompleto);
        var existente = await _repository.GetByNombreUsuarioAsync(nombreUsuario);
        if (existente is not null && existente.Id != usuario.Id)
            throw new BusinessRuleException("Ya existe otro usuario con ese nombre de usuario.");

        var rolAnterior = usuario.RolEntidad?.Nombre ?? "Sin rol válido";
        var rolNuevo = await ResolverRolDinamicoAsync(dto.RolId);
        var eraAdmin = usuario.RolEntidad?.EsAdministrador == true;
        var seraAdmin = rolNuevo.EsAdministrador;

        if (eraAdmin && !seraAdmin && _currentUser.UsuarioId == usuario.Id)
        {
            var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirUsuarioId: usuario.Id);
            if (otrosAdmins == 0)
                throw new BusinessRuleException("No puedes quitarte a ti mismo el rol de administrador: eres el único administrador activo del sistema.");
        }

        var datosAnteriores = new { usuario.NombreUsuario, usuario.NombreCompleto, Rol = rolAnterior, usuario.RolId };

        usuario.NombreUsuario = nombreUsuario;
        usuario.NombreCompleto = nombreCompleto;
        usuario.RolId = rolNuevo.Id;
        usuario.RolEntidad = rolNuevo;
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;

        var huboResetPassword = !string.IsNullOrWhiteSpace(dto.NuevaPassword);
        if (huboResetPassword)
        {
            ValidarPasswordSegura(dto.NuevaPassword!);
            if (BCrypt.Net.BCrypt.Verify(dto.NuevaPassword!, usuario.PasswordHash))
                throw new BusinessRuleException("La nueva contraseña debe ser diferente de la actual.");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword!, workFactor: 12);
        }

        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        if (datosAnteriores.RolId != rolNuevo.Id)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.AsignarRol,
                $"Cambió el rol del usuario '{usuario.NombreUsuario}' de '{rolAnterior}' a '{rolNuevo.Nombre}'.",
                usuario.Id,
                entidad: "Usuario",
                valoresAnteriores: new { RolId = datosAnteriores.RolId, Rol = rolAnterior },
                valoresNuevos: new { RolId = rolNuevo.Id, Rol = rolNuevo.Nombre });
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Editar,
            $"Editó los datos del usuario '{usuario.NombreUsuario}'.",
            usuario.Id,
            entidad: "Usuario",
            valoresAnteriores: new { datosAnteriores.NombreUsuario, datosAnteriores.NombreCompleto },
            valoresNuevos: new { usuario.NombreUsuario, usuario.NombreCompleto });

        if (huboResetPassword)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Usuarios,
                AccionPermiso.RestablecerContrasena,
                $"Restableció la contraseña del usuario '{usuario.NombreUsuario}'.",
                usuario.Id,
                entidad: "Usuario");
        }

        return ToDto(usuario);
    }

    public async Task<UsuarioDto?> UpdateEstadoAsync(int id, bool activo)
    {
        var usuario = await _repository.GetByIdAsync(id);
        if (usuario is null) return null;
        if (!activo) await ValidarNoEsUltimoAdminAsync(usuario, "desactivar");

        usuario.Activo = activo;
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"{(activo ? "Activó" : "Desactivó")} el usuario '{usuario.NombreUsuario}'.",
            usuario.Id,
            entidad: "Usuario");

        return ToDto(usuario);
    }

    public async Task<UsuarioDto> BloquearAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo del bloqueo es obligatorio.");

        var usuario = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario no existe.");
        if (usuario.Id == _currentUser.UsuarioId)
            throw new BusinessRuleException("No puedes bloquearte a ti mismo.");

        await ValidarNoEsUltimoAdminAsync(usuario, "bloquear");

        usuario.Bloqueado = true;
        usuario.MotivoBloqueo = motivo.Trim();
        usuario.FechaBloqueo = DateTime.UtcNow;
        usuario.BloqueadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Desactivar,
            $"Bloqueó al usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario", motivo: motivo);
        return ToDto(usuario);
    }

    public async Task<UsuarioDto> DesbloquearAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario no existe.");

        usuario.Bloqueado = false;
        usuario.MotivoBloqueo = null;
        usuario.FechaBloqueo = null;
        usuario.BloqueadoPorUsuarioId = null;
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.Activar,
            $"Desbloqueó al usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario");
        return ToDto(usuario);
    }

    public async Task EliminarAsync(int id)
    {
        var usuario = await _repository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario no existe.");
        if (usuario.Id == _currentUser.UsuarioId)
            throw new BusinessRuleException("No puedes eliminar tu propia cuenta.");

        await ValidarNoEsUltimoAdminAsync(usuario, "eliminar");
        usuario.Eliminado = true;
        usuario.Activo = false;
        usuario.FechaEliminacion = DateTime.UtcNow;
        usuario.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        usuario.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(usuario);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(ModuloSistema.Usuarios, AccionPermiso.EliminarLogico,
            $"Eliminó lógicamente al usuario '{usuario.NombreUsuario}'.", usuario.Id, entidad: "Usuario");
    }

    public async Task<List<UsuarioEmpresaDto>> GetEmpresasAsync(int usuarioId)
    {
        await ObtenerUsuarioValidoParaMembresiaAsync(usuarioId, exigirActivo: false);
        return (await _repository.GetEmpresasAsync(usuarioId)).Select(ToEmpresaDto).ToList();
    }

    public async Task<UsuarioEmpresaDto> AsignarEmpresaAsync(int usuarioId, AsignarUsuarioEmpresaDto dto)
    {
        var usuario = await ObtenerUsuarioValidoParaMembresiaAsync(usuarioId, exigirActivo: true);
        var empresa = await ObtenerEmpresaActivaAsync(dto.EmpresaId);
        var rol = await ResolverRolDinamicoAsync(dto.RolId);

        if (await _repository.GetEmpresaAsync(usuarioId, empresa.Id) is not null)
            throw new BusinessRuleException("El usuario ya tiene una membresía registrada para la empresa seleccionada.");

        var membresia = new UsuarioEmpresa(usuario.Id, empresa.Id, rol.Id)
        {
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddEmpresaAsync(membresia);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.AsignarRol,
            $"Asignó al usuario '{usuario.NombreUsuario}' a la empresa '{empresa.Nombre}' con rol '{rol.Nombre}'.",
            usuario.Id,
            entidad: "UsuarioEmpresa",
            valoresNuevos: new { membresia.UsuarioId, membresia.EmpresaId, membresia.RolId, membresia.Activa });

        return ToEmpresaDto(membresia);
    }

    public async Task<UsuarioEmpresaDto> CambiarRolEmpresaAsync(int usuarioId, int empresaId, int rolId)
    {
        var usuario = await ObtenerUsuarioValidoParaMembresiaAsync(usuarioId, exigirActivo: true);
        await ObtenerEmpresaActivaAsync(empresaId);
        var rol = await ResolverRolDinamicoAsync(rolId);
        var membresia = await ObtenerMembresiaAsync(usuarioId, empresaId);
        var rolAnterior = membresia.RolId;

        membresia.CambiarRol(rol.Id);
        membresia.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        membresia.FechaActualizacion = DateTime.UtcNow;
        _repository.UpdateEmpresa(membresia);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.AsignarRol,
            $"Cambió el rol empresarial del usuario '{usuario.NombreUsuario}' en EmpresaId={empresaId}.",
            usuario.Id,
            entidad: "UsuarioEmpresa",
            valoresAnteriores: new { RolId = rolAnterior, EmpresaId = empresaId },
            valoresNuevos: new { RolId = rol.Id, EmpresaId = empresaId });

        return ToEmpresaDto(membresia);
    }

    public async Task<UsuarioEmpresaDto> CambiarEstadoEmpresaAsync(int usuarioId, int empresaId, bool activa)
    {
        var usuario = await ObtenerUsuarioValidoParaMembresiaAsync(usuarioId, exigirActivo: activa);
        if (activa)
            await ObtenerEmpresaActivaAsync(empresaId);

        var membresia = await ObtenerMembresiaAsync(usuarioId, empresaId);
        if (activa)
        {
            await ResolverRolDinamicoAsync(membresia.RolId);
            membresia.Activar();
        }
        else
        {
            membresia.Desactivar();
        }

        membresia.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        membresia.FechaActualizacion = DateTime.UtcNow;
        _repository.UpdateEmpresa(membresia);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            activa ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"{(activa ? "Activó" : "Desactivó")} la membresía empresarial del usuario '{usuario.NombreUsuario}' en EmpresaId={empresaId}.",
            usuario.Id,
            entidad: "UsuarioEmpresa",
            valoresNuevos: new { membresia.EmpresaId, membresia.RolId, membresia.Activa });

        return ToEmpresaDto(membresia);
    }

    private async Task<Usuario> ObtenerUsuarioValidoParaMembresiaAsync(int usuarioId, bool exigirActivo)
    {
        if (usuarioId <= 0)
            throw new BusinessRuleException("El usuario seleccionado no es válido.");

        var usuario = await _repository.GetByIdAsync(usuarioId)
            ?? throw new BusinessRuleException("El usuario no existe.");

        if (exigirActivo && (!usuario.Activo || usuario.Bloqueado))
            throw new BusinessRuleException("No se puede administrar una membresía activa para un usuario inactivo o bloqueado.");

        return usuario;
    }

    private async Task<Empresa> ObtenerEmpresaActivaAsync(int empresaId)
    {
        if (empresaId <= 0)
            throw new BusinessRuleException("La empresa seleccionada no es válida.");

        var empresa = await _empresaRepository.GetByIdAsync(empresaId, CancellationToken.None)
            ?? throw new BusinessRuleException("La empresa seleccionada no existe.");

        if (!empresa.Activa || empresa.Eliminado)
            throw new BusinessRuleException("No se puede asignar una empresa inactiva o eliminada.");

        return empresa;
    }

    private async Task<UsuarioEmpresa> ObtenerMembresiaAsync(int usuarioId, int empresaId) =>
        await _repository.GetEmpresaAsync(usuarioId, empresaId)
        ?? throw new BusinessRuleException("El usuario no tiene una membresía registrada para la empresa seleccionada.");

    private async Task ValidarNoEsUltimoAdminAsync(Usuario usuario, string accion)
    {
        if (usuario.RolEntidad?.EsAdministrador != true) return;
        var otrosAdmins = await _repository.ContarAdministradoresActivosAsync(excluirUsuarioId: usuario.Id);
        if (otrosAdmins == 0)
            throw new BusinessRuleException($"No se puede {accion} este usuario: es el único administrador activo del sistema.");
    }

    private static string NormalizarNombreUsuario(string valor)
    {
        var normalizado = valor?.Trim() ?? string.Empty;
        if (!NombreUsuarioValido.IsMatch(normalizado))
            throw new BusinessRuleException("El nombre de usuario debe tener entre 3 y 50 caracteres y usar únicamente letras, números, punto, guion o guion bajo.");
        return normalizado;
    }

    private static string NormalizarNombreCompleto(string valor)
    {
        var normalizado = Regex.Replace(valor?.Trim() ?? string.Empty, "\\s+", " ");
        if (normalizado.Length < 3 || normalizado.Length > 150)
            throw new BusinessRuleException("El nombre completo debe tener entre 3 y 150 caracteres.");
        return normalizado;
    }

    private static void ValidarPasswordSegura(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10 || password.Length > 128)
            throw new BusinessRuleException("La contraseña debe tener entre 10 y 128 caracteres.");
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new BusinessRuleException("La contraseña debe incluir mayúscula, minúscula, número y símbolo.");
    }

    private static UsuarioDto ToDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.RolEntidad?.Nombre ?? string.Empty,
        RolId = u.RolId,
        Activo = u.Activo,
        Bloqueado = u.Bloqueado,
        FechaCreacion = u.FechaCreacion
    };

    private static UsuarioDetalleDto ToDetalleDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.RolEntidad?.Nombre ?? string.Empty,
        RolId = u.RolId,
        RolNombre = u.RolEntidad?.Nombre,
        Activo = u.Activo,
        Bloqueado = u.Bloqueado,
        MotivoBloqueo = u.MotivoBloqueo,
        FechaBloqueo = u.FechaBloqueo,
        FechaCreacion = u.FechaCreacion,
        FechaActualizacion = u.FechaActualizacion
    };

    private static UsuarioEmpresaDto ToEmpresaDto(UsuarioEmpresa x) => new()
    {
        Id = x.Id,
        UsuarioId = x.UsuarioId,
        EmpresaId = x.EmpresaId,
        RolId = x.RolId,
        Activa = x.Activa
    };
}
