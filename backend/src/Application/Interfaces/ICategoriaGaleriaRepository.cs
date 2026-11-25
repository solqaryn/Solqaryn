using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ICategoriaGaleriaRepository
{
    Task<CategoriaGaleria?> GetAsync(int empresaId, int categoriaId, CancellationToken cancellationToken);
    Task<List<CategoriaGaleria>> GetByCategoriasAsync(int empresaId, IReadOnlyCollection<int> categoriaIds, CancellationToken cancellationToken);
    Task SaveAsync(CategoriaGaleria galeria, bool nueva, CancellationToken cancellationToken);
}
