using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICajaService
{
    Task<CajaDto> GetCajaByIdAsync(int id);
    Task<CajaSesionDto> GetSesionByIdAsync(int id);
    Task<CajaSesionDto?> GetSesionActivaAsync(int cajaId);
    Task<CajaDto> CrearCajaAsync(CrearCajaDto dto);
    Task<CajaDto> ActivarCajaAsync(int id);
    Task<CajaDto> DesactivarCajaAsync(int id);
    Task<CajaSesionDto> AbrirSesionAsync(int cajaId, AbrirCajaSesionDto dto);
    Task<CajaSesionDto> IniciarOperacionesAsync(int sesionId);
    Task<CajaSesionDto> RegistrarMovimientoAsync(int sesionId, RegistrarMovimientoCajaDto dto);
    Task<CajaSesionDto> IniciarArqueoAsync(int sesionId);
    Task<CajaSesionDto> CerrarSesionAsync(int sesionId, CerrarCajaSesionDto dto);
}
