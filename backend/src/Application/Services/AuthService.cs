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
        if (!await HasActiveMembershipForOwnedRoleAsync(usuario))
            return null;
        return CrearRespuesta(usuario);
    }

    public async Task<LoginResponseDto?> RenovarAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null || usuario.Eliminado) return null;

        ValidarEstadoCuenta(usuario);
        ValidarRolRelacional(usuario);
        if (!await HasActiveMembershipForOwnedRoleAsync(usuario))
            return null;
        return CrearRespuesta(usuario);
    }


    // Tenant-owned roles must retain a live, matching SQL membership before
    // login or refresh can issue a JWT. No client-selected tenant is trusted.
    private async Task<bool> HasActiveMembershipForOwnedRoleAsync(Usuario usuario)
    {
        var role = usuario.RolEntidad;
        // Un rol desacoplado del RolId SQL jamás puede originar un JWT,
        // incluso cuando sea una plantilla empresarial sin dueño.
        if (usuario.Id <= 0 || role.Id <= 0 || usuario.RolId != role.Id)
            return false;

        if (role.EmpresaId is null)
        {
            // A system enterprise role is a reusable template, not a tenant
            // grant. Current SQL membership in that exact role is mandatory
            // for both login and renewal. Platform roles remain separate.
            if (role.Ambito == Solqaryn.Domain.Enums.AmbitoAutorizacion.Plataforma)
                return role.EsSistema;
            if (role.Ambito != Solqaryn.Domain.Enums.AmbitoAutorizacion.Empresa ||
                !role.EsSistema)
                return false;

            var memberships = await _usuarioRepository.GetAuthenticationMembershipsAsync(usuario.Id, role.Id, null);
            return memberships is { Count: > 0 } &&
                memberships.Any(m => m.Activa && m.UsuarioId == usuario.Id &&
                    m.EmpresaId > 0 && m.RolId == role.Id);
        }

        if (role.Ambito != Solqaryn.Domain.Enums.AmbitoAutorizacion.Empresa ||
            role.EmpresaId <= 0 || role.Id <= 0 || usuario.RolId != role.Id ||
            usuario.Id <= 0)
            return false;

        var membershipsOwned = await _usuarioRepository.GetAuthenticationMembershipsAsync(
            usuario.Id, role.Id, role.EmpresaId.Value);
        return membershipsOwned is { Count: 1 } &&
            membershipsOwned[0].Activa &&
            membershipsOwned[0].UsuarioId == usuario.Id &&
            membershipsOwned[0].EmpresaId == role.EmpresaId.Value &&
            membershipsOwned[0].RolId == role.Id;
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
