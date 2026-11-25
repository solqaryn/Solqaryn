using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class CategoriaGaleriaRepository(AppDbContext context) : ICategoriaGaleriaRepository
{
    public Task<CategoriaGaleria?> GetAsync(int empresaId, int categoriaId, CancellationToken cancellationToken) =>
        context.CategoriasGalerias.SingleOrDefaultAsync(g => g.EmpresaId == empresaId && g.CategoriaId == categoriaId, cancellationToken);

    public Task<List<CategoriaGaleria>> GetByCategoriasAsync(int empresaId, IReadOnlyCollection<int> categoriaIds, CancellationToken cancellationToken) =>
        context.CategoriasGalerias.AsNoTracking().Where(g => g.EmpresaId == empresaId && categoriaIds.Contains(g.CategoriaId)).ToListAsync(cancellationToken);

    public async Task SaveAsync(CategoriaGaleria galeria, bool nueva, CancellationToken cancellationToken)
    {
        if (nueva) context.CategoriasGalerias.Add(galeria);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleException("La galería cambió en otra sesión. Recarga la categoría antes de guardar.");
        }
        catch (DbUpdateException)
        {
            throw new BusinessRuleException("No fue posible guardar la galería. Recarga la categoría e intenta nuevamente.");
        }
    }
}
