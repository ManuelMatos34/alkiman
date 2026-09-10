namespace Alkiman.API.Middleware;

/// <summary>
/// Agrega cabeceras de seguridad HTTP estándar a todas las respuestas.
/// Reduce la superficie de ataque contra clickjacking, MIME sniffing y XSS reflejado.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Previene que el navegador adivine el MIME type (MIME sniffing)
        headers["X-Content-Type-Options"] = "nosniff";

        // Bloquea embebido en iframes (anti-clickjacking)
        headers["X-Frame-Options"] = "DENY";

        // Filtro XSS del navegador (legacy pero útil en navegadores antiguos)
        headers["X-XSS-Protection"] = "1; mode=block";

        // Controla qué información del Referer se envía a otros orígenes
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Deshabilita funciones del navegador que la API no necesita
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Para APIs que solo devuelven JSON, CSP más estricta es suficiente
        headers["Content-Security-Policy"] = "default-src 'none'";

        // Fuerza HTTPS por un año (solo aplica sobre conexiones HTTPS)
        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Evita que las respuestas con datos sensibles queden en caché
        headers["Cache-Control"] = "no-store";

        await next(context);
    }
}
