namespace Solqaryn.Application.Exceptions;

/// Excepción para violaciones de reglas de negocio (se traduce a 400 Bad Request).
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
