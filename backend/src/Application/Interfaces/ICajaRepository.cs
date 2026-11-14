using Solqaryn.Domain.Entities.Cajas;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Persistence boundary for N4.1.D Caja application flows.
/// Mutating reads must be executed inside an active transaction by the infrastructure implementation.
/// </summary>
public interface ICajaRepository
{
    Task<Caja?> GetCajaByIdAsync(int id, bool tracking = false);
    Task<Caja?> GetCajaByIdForUpdateAsync(int id);
    Task<CajaSesion?> GetSesionByIdAsync(int id, bool tracking = false);
    Task<CajaSesion?> GetSesionByIdForUpdateAsync(int id);
    Task<CajaSesion?> GetSesionActivaByCajaIdAsync(int cajaId, bool tracking = false);
    Task AddCajaAsync(Caja caja);
    Task AddSesionAsync(CajaSesion sesion);
    void UpdateCaja(Caja caja);
    void UpdateSesion(CajaSesion sesion);
    Task<bool> SaveChangesAsync();
}
