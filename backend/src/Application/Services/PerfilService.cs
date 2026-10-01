using System.Text.RegularExpressions;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class PerfilService : IPerfilService
{
    private static readonly Regex NombreUsuarioValido = new(
        "^[a-zA-Z0-9._-]{3,50}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IPerfilImagenStorageService _imagenStorage;

    public PerfilService(
        IUsuarioRepository usuarioRepository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        IPerfilImagenStorageService imagenStorage)
    {
        _usuarioRepository = usuarioRepository;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _imagenStorage = imagenStorage;
    }

    private async Task<Usuario> GetUsuarioActualAsync()
    {
        var id = _currentUser.UsuarioId
            ?? throw new BusinessRuleException("No se pudo identificar al usuario autenticado.");

        var usuario = await _usuarioRepository.GetByIdAsync(id)
            ?? throw new BusinessRuleException("El usuario autenticado ya no existe.");

        if (!usuario.Activo || usuario.Bloqueado || usuario.Eliminado)
            throw new BusinessRuleException("La cuenta no está disponible para realizar esta operación.");

        return usuario;
    }

    public async Task<PerfilDto> GetPerfilAsync()
    {
        var usuario = await GetUsuarioActualAsync();
        return ToDto(usuario);
    }

    public async Task<PerfilDto> ActualizarPerfilAsync(ActualizarPerfilDto dto)
    {
        var nombreUsuario = NormalizarNombreUsuario(dto.NombreUsuario);
        var nombreCompleto = NormalizarNombreCompleto(dto.NombreCompleto);
        var usuario = await GetUsuarioActualAsync();

        var existente = await _usuarioRepository.GetByNombreUsuarioAsync(nombreUsuario);
        if (existente is not null && existente.Id != usuario.Id)
            throw new BusinessRuleException("Ya existe otro usuario con ese nombre de acceso.");

        var anterior = new
        {
            usuario.NombreUsuario,
            usuario.NombreCompleto
        };

        usuario.NombreUsuario = nombreUsuario;
        usuario.NombreCompleto = nombreCompleto;
        usuario.ActualizadoPorUsuarioId = usuario.Id;
        usuario.FechaActualizacion = DateTime.UtcNow;

        _usuarioRepository.Update(usuario);
        await _usuarioRepository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Editar,
            $"El usuario '{usuario.NombreUsuario}' actualizó los datos de su perfil.",
            usuario.Id,
            entidad: "Usuario",
            valoresAnteriores: anterior,
            valoresNuevos: new { usuario.NombreUsuario, usuario.NombreCompleto });

        return ToDto(usuario);
    }

    public async Task<PerfilDto> ActualizarFotoAsync(ActualizarFotoPerfilDto dto)
    {
        if (dto.Foto is null)
            throw new BusinessRuleException("Selecciona una fotografía válida.");

        var usuario = await GetUsuarioActualAsync();
        var fotoAnteriorUrl = usuario.FotoPerfilUrl;
        var fotoAnteriorPublicId = usuario.FotoPerfilPublicId;
        var nueva = await _imagenStorage.UploadAsync(dto.Foto);

        try
        {
            usuario.FotoPerfilUrl = nueva.Url;
            usuario.FotoPerfilPublicId = nueva.PublicId;
            usuario.ActualizadoPorUsuarioId = usuario.Id;
            usuario.FechaActualizacion = DateTime.UtcNow;

            _usuarioRepository.Update(usuario);
            await _usuarioRepository.SaveChangesAsync();
        }
        catch
        {
            await _imagenStorage.DeleteAsync(nueva.PublicId);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(fotoAnteriorPublicId) &&
            !string.Equals(fotoAnteriorPublicId, nueva.PublicId, StringComparison.Ordinal))
        {
            await _imagenStorage.DeleteAsync(fotoAnteriorPublicId);
        }

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Editar,
            $"El usuario '{usuario.NombreUsuario}' actualizó su fotografía de perfil.",
            usuario.Id,
            entidad: "Usuario",
            valoresAnteriores: new { FotoPerfilUrl = fotoAnteriorUrl },
            valoresNuevos: new { usuario.FotoPerfilUrl });

        return ToDto(usuario);
    }

    public async Task<PerfilDto> EliminarFotoAsync()
    {
        var usuario = await GetUsuarioActualAsync();
        var fotoAnteriorUrl = usuario.FotoPerfilUrl;
        var fotoAnteriorPublicId = usuario.FotoPerfilPublicId;

        if (string.IsNullOrWhiteSpace(fotoAnteriorUrl) && string.IsNullOrWhiteSpace(fotoAnteriorPublicId))
            return ToDto(usuario);

        usuario.FotoPerfilUrl = null;
        usuario.FotoPerfilPublicId = null;
        usuario.ActualizadoPorUsuarioId = usuario.Id;
        usuario.FechaActualizacion = DateTime.UtcNow;

        _usuarioRepository.Update(usuario);
        await _usuarioRepository.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(fotoAnteriorPublicId))
            await _imagenStorage.DeleteAsync(fotoAnteriorPublicId);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.Editar,
            $"El usuario '{usuario.NombreUsuario}' eliminó su fotografía de perfil.",
            usuario.Id,
            entidad: "Usuario",
            valoresAnteriores: new { FotoPerfilUrl = fotoAnteriorUrl },
            valoresNuevos: new { FotoPerfilUrl = (string?)null });

        return ToDto(usuario);
    }

    public async Task CambiarPasswordAsync(CambiarPasswordPropiaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PasswordActual))
            throw new BusinessRuleException("La contraseña actual es obligatoria.");

        ValidarPasswordSegura(dto.PasswordNueva);
        var usuario = await GetUsuarioActualAsync();

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
            throw new BusinessRuleException("La contraseña actual no es correcta.");

        if (BCrypt.Net.BCrypt.Verify(dto.PasswordNueva, usuario.PasswordHash))
            throw new BusinessRuleException("La nueva contraseña debe ser diferente de la actual.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva, workFactor: 12);
        usuario.ActualizadoPorUsuarioId = usuario.Id;
        usuario.FechaActualizacion = DateTime.UtcNow;

        _usuarioRepository.Update(usuario);
        await _usuarioRepository.SaveChangesAsync();

        // Nunca se registran contraseñas, hashes ni longitudes en auditoría.
        await _auditoria.RegistrarAsync(
            ModuloSistema.Usuarios,
            AccionPermiso.RestablecerContrasena,
            $"El usuario '{usuario.NombreUsuario}' cambió su propia contraseña.",
            usuario.Id,
            entidad: "Usuario");
    }

    private static string NormalizarNombreUsuario(string valor)
    {
        var normalizado = valor?.Trim() ?? string.Empty;
        if (!NombreUsuarioValido.IsMatch(normalizado))
            throw new BusinessRuleException(
                "El nombre de usuario debe tener entre 3 y 50 caracteres y usar únicamente letras, números, punto, guion o guion bajo.");

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
            throw new BusinessRuleException("La nueva contraseña debe tener entre 10 y 128 caracteres.");

        if (!password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new BusinessRuleException(
                "La nueva contraseña debe incluir mayúscula, minúscula, número y símbolo.");
        }
    }

    private static PerfilDto ToDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        NombreCompleto = u.NombreCompleto,
        Rol = u.RolEntidad?.Nombre ?? u.Rol.ToString(),
        FotoPerfilUrl = u.FotoPerfilUrl,
        FechaCreacion = u.FechaCreacion
    };
}
