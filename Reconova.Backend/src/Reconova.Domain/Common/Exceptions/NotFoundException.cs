namespace Reconova.Domain.Common.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base("NOT_FOUND", $"{entityName} with key '{key}' was not found.")
    {
    }
}
