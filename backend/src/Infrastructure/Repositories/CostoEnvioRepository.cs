using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Exceptions;
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

    public Task<bool> ExisteIncluidoEliminadoAsync(int id) =>
        _context.CostosEnvio.AnyAsync(x => x.Id == id);

    public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> accion)
    {
        var empresa = _context.AuthorizedEmpresaId ?? throw new ForbiddenAccessException("Se requiere empresa autorizada.");
        await using var tx = await _context.Database.BeginTransactionAsync();
        // Serialize administrative tariff mutations by the verified tenant row.
        var locked = await _context.Database.SqlQueryRaw<int>(
            "SELECT Id AS Value FROM Empresas WHERE Id={0} FOR UPDATE", empresa).ToListAsync();
        if (locked.Count != 1) throw new ForbiddenAccessException("Empresa no disponible.");
        var result = await accion();
        await tx.CommitAsync();
        return result;
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
            .OrderBy(x => x.Prioridad).ThenBy(x => x.Id)
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
