namespace Alkiman.API.RateLimiting;

/// <summary>
/// Nombres de las políticas de rate limiting registradas en Program.cs.
/// Se usan en los atributos [EnableRateLimiting] de los controllers.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Endpoints de autenticación (login, register, forgot-password).
    /// Ventana deslizante muy estricta para mitigar ataques de fuerza bruta.
    /// Límite: 10 requests por minuto por IP.
    /// </summary>
    public const string Auth = "auth";

    /// <summary>
    /// Endpoints públicos sin autenticación (portales, kioscos de pago).
    /// Límite moderado para permitir carga legítima sin dejar abierto el abuso.
    /// Límite: 60 requests por minuto por IP.
    /// </summary>
    public const string Public = "public";
}
