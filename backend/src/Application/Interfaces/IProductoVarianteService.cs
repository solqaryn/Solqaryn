using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IProductoVarianteService
{
    Task<List<ProductoVarianteDto>> GetByProductoIdAsync(int productoId, bool incluirInactivas = true);
    Task<ProductoVarianteDto?> GetByIdAsync(int productoId, int id);
    Task<ProductoVarianteDto> CreateAsync(int productoId, CreateProductoVarianteDto dto);
    Task<ProductoVarianteDto?> UpdateAsync(int productoId, int id, UpdateProductoVarianteDto dto);
    Task<ProductoVarianteDto?> CambiarEstadoAsync(int productoId, int id, bool activo);
    Task<bool> DeleteAsync(int productoId, int id);
    Task<ProductoVarianteDto> AsegurarTecnicaAsync(int productoId);
    Task<ProductoVarianteDto> SincronizarTecnicaAsync(int productoId, ProductoVarianteFormularioDto dto);
    Task RetirarTecnicaParaConversionAsync(int productoId);
    Task SincronizarTecnicaConProductoAsync(int productoId);
    Task EliminarTecnicaConProductoAsync(int productoId);
}
