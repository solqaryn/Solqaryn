using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IPedidoVentaService
{
    Task<PedidoVentaDto> GetByIdAsync(int id);
    Task<PagedResult<PedidoVentaDto>> GetPagedAsync(PedidoVentaFiltroDto request);
    Task<PedidoVentaDto> CrearDesdeCotizacionAsync(CreatePedidoVentaDto dto, string idempotencyKey);
    Task<PedidoVentaDto> ActualizarAsync(UpdatePedidoVentaDto dto);
    Task<PedidoVentaDto> ConfirmarAsync(int id, ConfirmarPedidoVentaDto dto);
    Task<PedidoVentaDto> AnularAsync(int id, string motivo);
}
