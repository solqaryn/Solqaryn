using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Catalogos;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class MetodoPagoRepository : IMetodoPagoRepository
{
    private readonly AppDbContext _context;
    private DbSet<MetodoPago> Set => _context.Set<MetodoPago>();

    public MetodoPagoRepository(AppDbContext context) => _context = context;

    public Task<MetodoPago?> GetByIdAsync(int id) => Set.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<List<MetodoPago>> GetAllAsync() =>
        MetodoPago.OrdenarParaSeleccion(await Set.AsNoTracking().ToListAsync()).ToList();

    public async Task<List<MetodoPago>> GetActivosAsync() =>
        MetodoPago.OrdenarParaSeleccion(await Set.AsNoTracking().Where(x => x.Activo).ToListAsync()).ToList();

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null)
    {
        var normalizado = codigo.Trim().ToLower();
        return Set.AnyAsync(x => x.Codigo.ToLower() == normalizado && (excluirId == null || x.Id != excluirId));
    }

    public Task AddAsync(MetodoPago metodoPago) => Set.AddAsync(metodoPago).AsTask();
    public void Update(MetodoPago metodoPago) => Set.Update(metodoPago);
    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
