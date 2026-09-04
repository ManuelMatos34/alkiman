using System.Net;
using Alkiman.Application.Common.Exceptions;

namespace Alkiman.API.Middleware;

/// <summary>Traduce las excepciones de la capa Application a respuestas HTTP con formato ProblemDetails.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, title) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, "Recurso no encontrado"),
                ForbiddenException => (HttpStatusCode.Forbidden, "Acceso no permitido"),
                AppValidationException => (HttpStatusCode.BadRequest, "Solicitud inválida"),
                _ => (HttpStatusCode.InternalServerError, "Error interno del servidor")
            };

            var isUnexpected = statusCode == HttpStatusCode.InternalServerError;

            if (isUnexpected)
                _logger.LogError(ex, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsJsonAsync(new
            {
                type = $"https://httpstatuses.io/{(int)statusCode}",
                title,
                status = (int)statusCode,
                // Los mensajes de las excepciones de Application están escritos para que
                // los lea el usuario. El de una excepción inesperada no: un SqlException
                // le nombra al cliente la tabla, la columna y el foreign key que falló.
                // Eso queda en el log, que es donde sirve.
                detail = isUnexpected
                    ? "Ocurrió un error inesperado. Si vuelve a pasar, avisale a soporte."
                    : ex.Message
            });
        }
    }
}
