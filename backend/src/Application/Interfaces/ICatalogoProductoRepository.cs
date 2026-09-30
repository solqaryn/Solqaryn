using Solqaryn.Application.Models;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface ICatalogoProductoRepository
{
    Task<List<MaestroProductoRegistro>> GetAllAsync(
        TipoCatalogoProducto tipo,
        string? buscar = null,
        int? catalogoPadreId = null);

    Task<List<MaestroProductoRegistro>> GetActivosAsync(
        TipoCatalogoProducto tipo,
        int? catalogoPadreId = null);

    Task<MaestroProductoRegistro?> GetByIdAsync(TipoCatalogoProducto tipo, int id);
    Task<MaestroProductoRegistro?> GetByIdConRelacionesAsync(TipoCatalogoProducto tipo, int id);
    Task<bool> ExisteNombreAsync(
        TipoCatalogoProducto tipo,
        string nombre,
        int? catalogoPadreId,
        int? excluirId = null);

    Task<int> AddAsync(MaestroProductoRegistro catalogo);
    Task<bool> UpdateAsync(MaestroProductoRegistro catalogo);
}
