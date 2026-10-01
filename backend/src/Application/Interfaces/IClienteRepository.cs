using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id);
    Task<Cliente?> GetByIdConVentasAsync(int id);
    Task<List<Cliente>> GetAllAsync();
    Task<List<Cliente>> GetActivosAsync();
    Task<List<Cliente>> BuscarActivosAsync(string termino, int limite = 10);
    Task<Cliente?> BuscarCoincidenciaActivaAsync(string? identidadORTN, string? correo, string? telefono, string? nombre);
    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null);
    Task<bool> ExisteIdentidadAsync(string identidadORTN, int? excluirId = null);
    Task AddAsync(Cliente cliente);
    void Update(Cliente cliente);
    void Remove(Cliente cliente);
    Task<bool> SaveChangesAsync();
}
