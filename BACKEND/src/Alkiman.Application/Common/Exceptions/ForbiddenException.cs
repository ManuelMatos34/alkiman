namespace Alkiman.Application.Common.Exceptions;

/// <summary>Se lanza cuando el landlord autenticado intenta acceder a un recurso que no le pertenece. Se traduce a HTTP 403.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
