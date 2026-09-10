using Alkiman.API.Security;

namespace Alkiman.API.Middleware;

/// <summary>
/// Bloquea peticiones de IPs en lista negra (estática o auto-detectadas por AbuseTracker).
///
/// Lista estática: se configura en Security:BlockedIps del appsettings.
/// Lista dinámica: IPs auto-bloqueadas por volumen excesivo de requests.
///
/// Corre muy al inicio del pipeline (antes del rate limiter) para cortar
/// la conexión lo más rápido posible sin consumir recursos del stack completo.
/// También registra cada petición legítima en AbuseTracker para detección de abuso.
/// </summary>
public sealed class IpBlocklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _staticBlocklist;

    public IpBlocklistMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _staticBlocklist = config
            .GetSection("Security:BlockedIps")
            .Get<string[]>()
            ?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = GetClientIp(context);

        if (_staticBlocklist.Contains(ip) || AbuseTracker.IsBlocked(ip))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                """{"status":403,"title":"Forbidden","detail":"Access denied."}""");
            return;
        }

        await _next(context);

        // Registrar después de procesar la solicitud para no bloquear la respuesta
        AbuseTracker.Record(ip);
    }

    private static string GetClientIp(HttpContext context)
    {
        // X-Forwarded-For puede tener una lista: tomamos el primero (cliente original)
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
