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

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Error no controlado procesando {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsJsonAsync(new
            {
                type = $"https://httpstatuses.io/{(int)statusCode}",
                title,
                status = (int)statusCode,
                detail = ex.Message
            });
        }
    }
}
