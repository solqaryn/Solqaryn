using Solqaryn.Domain.Entities;
using CatalogoBanco = Solqaryn.Domain.Entities.Catalogos.Banco;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Interfaces;

public interface IFacturaRepository
{
    Task<Factura?> GetByIdAsync(int id);
    Task<Factura?> GetByVentaIdAsync(int ventaId);
    Task<List<Factura>> GetAllAsync();
    Task<CatalogoMetodoPago?> GetMetodoPagoPorCodigoONombreAsync(string valor);
    Task<CatalogoBanco?> GetBancoActivoPorIdAsync(int id);
    Task<List<CatalogoBanco>> GetBancosActivosAsync();

    /// <summary>
    /// Recupera la factura sin aplicar el alcance del usuario autenticado.
    /// Solo puede usarse después de validar un enlace público por su hash,
    /// expiración, revocación y límite de accesos.
    /// </summary>
    Task<Factura?> GetByIdParaEnlacePublicoValidadoAsync(int id);

    Task AddAsync(Factura factura);
    void Update(Factura factura);
    Task<bool> SaveChangesAsync();
}
