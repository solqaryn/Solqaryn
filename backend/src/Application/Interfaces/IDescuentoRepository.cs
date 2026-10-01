using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IDescuentoRepository
{
    Task<Descuento?> GetByIdAsync(int id);
    Task<Descuento?> GetByIdConRelacionesAsync(int id);
    Task<List<Descuento>> GetAllAsync(bool incluirEliminados = false);
    Task<bool> ExisteCodigoAsync(string codigoNormalizado, int? excluirId = null);
    Task<int> ContarUsosAsync(int descuentoId);

    /// Todos los descuentos activos, no eliminados y vigentes por fecha (independiente
    /// de alcance). El motor de cálculo filtra por alcance/límites en memoria porque
    /// las reglas de acumulación y prioridad requieren evaluarlas juntas.
    Task<List<Descuento>> GetVigentesConRelacionesAsync(DateTime fecha);

    Task AddAsync(Descuento descuento);
    void Update(Descuento descuento);
    void Remove(Descuento descuento);
    Task AddHistorialAsync(HistorialUsoDescuento historial);
    Task<int> ContarUsosPorClienteAsync(int descuentoId, int clienteId);
    Task SaveChangesAsync();
}
