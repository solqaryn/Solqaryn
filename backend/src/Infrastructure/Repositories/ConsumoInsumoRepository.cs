using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class ConsumoInsumoRepository : IConsumoInsumoRepository
{
    private readonly AppDbContext _context;

    public ConsumoInsumoRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<ConsumoInsumo> ConDetalles() =>
        _context.ConsumosInsumos
            .Include(c => c.Detalles)
            .AsSplitQuery();

    public async Task<List<ConsumoInsumo>> GetAllAsync() =>
        await ConDetalles()
            .OrderByDescending(c => c.FechaConsumo)
            .ThenByDescending(c => c.Id)
            .ToListAsync();

    public async Task<ConsumoInsumo?> GetByIdAsync(int id) =>
        await ConDetalles().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<ConsumoInsumo?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var consumo = await _context.ConsumosInsumos
            .FromSqlInterpolated($"SELECT c.* FROM ConsumosInsumos c WHERE c.Id = {id} AND c.Eliminado = 0 FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (consumo is not null)
        {
            await _context.Entry(consumo)
                .Collection(c => c.Detalles)
                .LoadAsync();
        }

        return consumo;
    }

    public async Task AddAsync(ConsumoInsumo consumo) =>
        await _context.ConsumosInsumos.AddAsync(consumo);

    public void Update(ConsumoInsumo consumo) =>
        _context.ConsumosInsumos.Update(consumo);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
