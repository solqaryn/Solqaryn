using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IEmpresaConfiguracionRepository
{
    Task<EmpresaConfiguracion?> GetActivaAsync();
    Task AddAsync(EmpresaConfiguracion config);
    void Update(EmpresaConfiguracion config);

    Task<ConfigEmpresa?> GetTenantAsync(int empresaId, CancellationToken cancellationToken = default);
    Task AddTenantAsync(ConfigEmpresa config, CancellationToken cancellationToken = default);
    void UpdateTenant(ConfigEmpresa config);
    void DetachTenant(ConfigEmpresa config);

    Task<List<PlantillaCorreoEmpresa>> ListPlantillasAsync(int empresaId, CancellationToken cancellationToken = default);
    Task<PlantillaCorreoEmpresa?> GetPlantillaAsync(int empresaId, string tipoPlantilla, CancellationToken cancellationToken = default);
    Task AddPlantillaAsync(PlantillaCorreoEmpresa plantilla, CancellationToken cancellationToken = default);
    void UpdatePlantilla(PlantillaCorreoEmpresa plantilla);

    Task<bool> SaveChangesAsync();
}
