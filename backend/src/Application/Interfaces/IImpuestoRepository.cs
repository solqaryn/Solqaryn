using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public interface IImpuestoRepository
{
    Task<Impuesto?> GetByIdAsync(int id);
    Task<Impuesto?> GetByIdConRelacionesAsync(int id);
    Task<List<Impuesto>> GetAllAsync(bool incluirEliminados = false);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null);
    Task<int> ContarAplicacionesAsync(int impuestoId);
    Task<List<Impuesto>> GetVigentesConRelacionesAsync(DateTime fecha, OperacionImpuesto operacion);
    Task AddAsync(Impuesto impuesto);
    void Update(Impuesto impuesto);
    void Remove(Impuesto impuesto);
    Task AddHistorialAsync(HistorialAplicacionImpuesto historial);
    Task SaveChangesAsync();
}
