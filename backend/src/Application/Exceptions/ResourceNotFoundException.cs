namespace Solqaryn.Application.Exceptions;

public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}
