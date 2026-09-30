using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IRolRepository rolRepository,
        IJwtService jwtService)
    {
        _usuarioRepository = usuarioRepository;
        _jwtService = jwtService;
        _ = rolRepository; // Compatibilidad de constructor; RolId ya fue backfilleado por N0.4.
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var nombreUsuario = dto.NombreUsuario?.Trim() ?? string.Empty;
        var usuario = await _usuarioRepository.GetByNombreUsuarioAsync(nombreUsuario);
        if (usuario is null || usuario.Eliminado) return null;

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
            return null;

        ValidarEstadoCuenta(usuario);
        ValidarRolRelacional(usuario);
        return CrearRespuesta(usuario);
    }

    public async Task<LoginResponseDto?> RenovarAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null || usuario.Eliminado) return null;

        ValidarEstadoCuenta(usuario);
        ValidarRolRelacional(usuario);
        return CrearRespuesta(usuario);
    }

    private LoginResponseDto CrearRespuesta(Usuario usuario)
    {
        var (token, expiraEn) = _jwtService.GenerarToken(usuario);
        return new LoginResponseDto
        {
            Token = token,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.RolEntidad.Nombre,
            FotoPerfilUrl = usuario.FotoPerfilUrl,
            ExpiraEn = expiraEn
        };
    }

    private static void ValidarEstadoCuenta(Usuario usuario)
    {
        if (usuario.Bloqueado)
            throw new BusinessRuleException($"Esta cuenta está bloqueada. Motivo: {usuario.MotivoBloqueo ?? "no especificado"}. Contacta a un administrador.");
        if (!usuario.Activo)
            throw new BusinessRuleException("Esta cuenta está desactivada. Contacta a un administrador.");
    }

    private static void ValidarRolRelacional(Usuario usuario)
    {
        if (usuario.RolId <= 0 || usuario.RolEntidad is null || usuario.RolEntidad.Eliminado || !usuario.RolEntidad.Activo)
            throw new BusinessRuleException("La cuenta no tiene un rol relacional activo válido. Contacta a un administrador.");
    }
}
