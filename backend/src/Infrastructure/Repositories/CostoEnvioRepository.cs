using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class CostoEnvioRepository : ICostoEnvioRepository
{
    private readonly AppDbContext _context;

    public CostoEnvioRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<CostoEnvio>> GetAllAsync() =>
        _context.CostosEnvio
            .Where(x => !x.Eliminado)
            .OrderBy(x => x.Prioridad)
            .ThenBy(x => x.Nombre)
            .ToListAsync();

    public Task<CostoEnvio?> GetByIdAsync(int id) =>
        _context.CostosEnvio.FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado);

    public Task<CostoEnvio?> GetPredeterminadoVigenteAsync(DateTime fecha) =>
        _context.CostosEnvio
            .Where(x => !x.Eliminado && x.Activo && x.EsPredeterminado)
            .Where(x => !x.VigenteDesde.HasValue || x.VigenteDesde <= fecha)
            .Where(x => !x.VigenteHasta.HasValue || x.VigenteHasta >= fecha)
            .OrderBy(x => x.Prioridad)
            .FirstOrDefaultAsync();

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, int? excluirId = null) =>
        _context.CostosEnvio.AnyAsync(x =>
            !x.Eliminado &&
            x.Nombre.ToUpper() == nombreNormalizado &&
            (!excluirId.HasValue || x.Id != excluirId.Value));

    public async Task DesmarcarPredeterminadosAsync(int? excluirId = null)
    {
        var items = await _context.CostosEnvio
            .Where(x => !x.Eliminado && x.EsPredeterminado && (!excluirId.HasValue || x.Id != excluirId.Value))
            .ToListAsync();

        foreach (var item in items)
        {
            item.EsPredeterminado = false;
            item.FechaActualizacion = DateTime.UtcNow;
        }
    }

    public Task AddAsync(CostoEnvio costoEnvio) =>
        _context.CostosEnvio.AddAsync(costoEnvio).AsTask();

    public void Update(CostoEnvio costoEnvio) =>
        _context.CostosEnvio.Update(costoEnvio);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
