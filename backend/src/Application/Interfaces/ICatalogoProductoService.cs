using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface ICatalogoProductoService
{
    Task<List<CatalogoProductoDto>> GetAllAsync(TipoCatalogoProducto tipo, string? buscar = null, int? catalogoPadreId = null);
    Task<List<CatalogoProductoDto>> GetActivosAsync(TipoCatalogoProducto tipo, int? catalogoPadreId = null);
    Task<CatalogoProductoDto?> GetByIdAsync(TipoCatalogoProducto tipo, int id);
    Task<CatalogoProductoDto> CreateAsync(TipoCatalogoProducto tipo, CreateCatalogoProductoDto dto);
    Task<CatalogoProductoDto?> UpdateAsync(TipoCatalogoProducto tipo, int id, UpdateCatalogoProductoDto dto);
    Task<CatalogoProductoDto?> CambiarEstadoAsync(TipoCatalogoProducto tipo, int id, bool activo);
    Task<bool> DeleteAsync(TipoCatalogoProducto tipo, int id);
    Task ValidarSeleccionProductoAsync(int? colorId, int? tallaId, int? marcaId, int? modeloId);
}
