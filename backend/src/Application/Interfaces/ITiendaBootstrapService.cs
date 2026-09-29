using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface ITiendaBootstrapService
{
    Task<TiendaBootstrapDto> ObtenerAsync(CancellationToken cancellationToken = default);
}
