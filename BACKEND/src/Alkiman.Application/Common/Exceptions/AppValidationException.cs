namespace Alkiman.Application.Common.Exceptions;

/// <summary>Se lanza cuando una regla de negocio de la capa Application no se cumple. Se traduce a HTTP 400.</summary>
public class AppValidationException : Exception
{
    public AppValidationException(string message) : base(message)
    {
    }
}
