using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IFacturaCompartirRepository
{
    Task<EnlacePublicoFactura?> GetPorTokenHashAsync(string tokenHash);
    Task<int> ExpirarVigentesAsync(int facturaId, DateTime fechaExpiracion);
    Task AddEnlaceAsync(EnlacePublicoFactura enlace);
    void UpdateEnlace(EnlacePublicoFactura enlace);
    Task AddHistorialAsync(HistorialEnvioFactura historial);
    Task<List<HistorialEnvioFactura>> GetHistorialAsync(int facturaId);
    Task SaveChangesAsync();
}