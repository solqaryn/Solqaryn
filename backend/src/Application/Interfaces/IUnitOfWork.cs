namespace Solqaryn.Application.Interfaces;

/// Ejecuta una operación dentro de una transacción real de base de datos.
/// Si la operación lanza una excepción, todo se revierte (rollback automático).
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> operation);
}
