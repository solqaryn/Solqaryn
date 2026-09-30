namespace Solqaryn.Application.Exceptions;

public class UniqueConstraintViolationException : Exception
{
    public string ConstraintName { get; }

    public UniqueConstraintViolationException(string constraintName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ConstraintName = constraintName;
    }
}
