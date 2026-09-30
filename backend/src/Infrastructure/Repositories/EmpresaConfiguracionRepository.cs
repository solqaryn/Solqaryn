using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class EmpresaConfiguracionRepository : IEmpresaConfiguracionRepository
{
    private readonly AppDbContext _context;

    public EmpresaConfiguracionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmpresaConfiguracion?> GetActivaAsync() =>
        await _context.EmpresaConfiguraciones.FirstOrDefaultAsync(e => e.Activa);

    public async Task AddAsync(EmpresaConfiguracion config) =>
        await _context.EmpresaConfiguraciones.AddAsync(config);

    public void Update(EmpresaConfiguracion config) =>
        _context.EmpresaConfiguraciones.Update(config);

    public Task<ConfigEmpresa?> GetTenantAsync(int empresaId, CancellationToken cancellationToken = default) =>
        _context.Set<ConfigEmpresa>()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, cancellationToken);

    public async Task AddTenantAsync(ConfigEmpresa config, CancellationToken cancellationToken = default) =>
        await _context.Set<ConfigEmpresa>().AddAsync(config, cancellationToken);

    public void UpdateTenant(ConfigEmpresa config) =>
        _context.Set<ConfigEmpresa>().Update(config);

    public void DetachTenant(ConfigEmpresa config) =>
        _context.Entry(config).State = EntityState.Detached;

    public Task<List<PlantillaCorreoEmpresa>> ListPlantillasAsync(int empresaId, CancellationToken cancellationToken = default) =>
        _context.Set<PlantillaCorreoEmpresa>()
            .AsNoTracking()
            .Where(p => p.EmpresaId == empresaId)
            .OrderBy(p => p.TipoPlantilla)
            .ToListAsync(cancellationToken);

    public Task<PlantillaCorreoEmpresa?> GetPlantillaAsync(int empresaId, string tipoPlantilla, CancellationToken cancellationToken = default) =>
        _context.Set<PlantillaCorreoEmpresa>()
            .FirstOrDefaultAsync(
                p => p.EmpresaId == empresaId && p.TipoPlantilla == tipoPlantilla,
                cancellationToken);

    public async Task AddPlantillaAsync(PlantillaCorreoEmpresa plantilla, CancellationToken cancellationToken = default) =>
        await _context.Set<PlantillaCorreoEmpresa>().AddAsync(plantilla, cancellationToken);

    public void UpdatePlantilla(PlantillaCorreoEmpresa plantilla) =>
        _context.Set<PlantillaCorreoEmpresa>().Update(plantilla);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
