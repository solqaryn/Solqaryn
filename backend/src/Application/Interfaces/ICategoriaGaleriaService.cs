using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICategoriaGaleriaService
{
    Task<CategoriaGaleriaDto?> ObtenerAsync(StorageTenantContext tenant, int categoriaId, CancellationToken cancellationToken);
    Task<CategoriaGaleriaDto?> GuardarAsync(StorageTenantContext tenant, int categoriaId, ActualizarCategoriaGaleriaDto dto, CancellationToken cancellationToken);
    Task<Dictionary<int, string[]>> ObtenerPublicasAsync(int empresaId, IReadOnlyCollection<int> categoriaIds, CancellationToken cancellationToken);
}
