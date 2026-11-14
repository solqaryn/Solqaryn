using Solqaryn.Application.Interfaces;

namespace Solqaryn.Tests;

/// Unit of work de prueba: ejecuta la operación directamente, sin transacción real de BD
/// (los tests usan mocks de repositorio, no una base de datos real).
public class FakeUnitOfWork : IUnitOfWork
{
    public async Task ExecuteInTransactionAsync(Func<Task> operation) => await operation();
}
