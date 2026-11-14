using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Interfaces;

public interface IEmpresaConfiguracionService
{
    Task<EmpresaConfiguracionDto> GetActivaAsync();
    Task<EmpresaConfiguracion> GetActivaEntidadAsync();
    Task<EmpresaConfiguracionDto> UpdateAsync(UpdateEmpresaConfiguracionDto dto);
    Task<EmpresaConfiguracionDto> UpdateLogoAsync(IFormFile logo);
    Task<EmpresaConfiguracionDto> RestaurarLogoAsync();

    Task<ConfigEmpresaTenantDto> GetTenantAsync(int empresaId, CancellationToken cancellationToken = default);
    Task<ConfigEmpresaTenantDto> UpdateTenantAsync(int empresaId, UpdateConfigEmpresaTenantDto dto, CancellationToken cancellationToken = default);
    Task<ConfigEmpresaTenantDto> UpdateTenantLogoAsync(int empresaId, IFormFile logo, CancellationToken cancellationToken = default);
    Task<ConfigEmpresaTenantDto> RestaurarTenantLogoAsync(int empresaId, CancellationToken cancellationToken = default);
    Task<ConfigEmpresaTenantDto> UpsertPlantillaTenantAsync(int empresaId, string tipoPlantilla, UpdatePlantillaCorreoEmpresaDto dto, CancellationToken cancellationToken = default);
    Task<ConfigEmpresaTenantDto> DesactivarPlantillaTenantAsync(int empresaId, string tipoPlantilla, long version, CancellationToken cancellationToken = default);
}