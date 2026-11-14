namespace Solqaryn.Application.Exceptions;

/// <summary>
/// Señala que otra solicitud ganó concurrentemente la clave durable de idempotencia.
/// El caso de uso debe releer el ledger autoritativo y decidir replay o conflicto
/// según el payload ya persistido; no debe exponer el error de persistencia al cliente.
/// </summary>
public sealed class IdempotencyConcurrencyException : Exception
{
    public IdempotencyConcurrencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
