using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IPreparacionPedidoVentaRepository
{
    Task<PreparacionPedidoVenta?> GetByIdAsync(int id, bool asNoTracking = false);
    Task<PreparacionPedidoVenta?> GetByPedidoVentaIdAsync(int pedidoVentaId, bool asNoTracking = false);
    Task<PreparacionPedidoVenta?> GetByIdForUpdateAsync(int id);
    Task<PreparacionPedidoVenta?> GetByPedidoVentaIdForUpdateAsync(int pedidoVentaId);
    Task AddAsync(PreparacionPedidoVenta preparacion);
    void Update(PreparacionPedidoVenta preparacion);
    Task<bool> SaveChangesAsync();
}

public interface IPreparacionPedidoVentaService
{
    Task<PreparacionPedidoVentaDto> GetByIdAsync(int id);
    Task<PreparacionPedidoVentaDto> GetByPedidoVentaIdAsync(int pedidoVentaId);
    Task<PreparacionPedidoVentaDto> IniciarAsync(int pedidoVentaId);
    Task<PreparacionPedidoVentaDto> CompletarPickingAsync(int id);
    Task<PreparacionPedidoVentaDto> CompletarPackingAsync(int id);
    Task<PreparacionPedidoVentaDto> MarcarDespachadoAsync(int id);
    Task<PreparacionPedidoVentaDto> MarcarEntregadoAsync(int id);
    Task<PreparacionPedidoVentaDto> CancelarAsync(int id, string motivo);
}
