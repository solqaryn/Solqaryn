using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<LoginResponseDto?> RenovarAsync(int usuarioId);
}
