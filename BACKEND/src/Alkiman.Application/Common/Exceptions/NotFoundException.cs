namespace Alkiman.Application.Common.Exceptions;

/// <summary>Se lanza cuando un recurso solicitado no existe. Se traduce a HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"'{entityName}' con clave '{key}' no fue encontrado.")
    {
    }
}
