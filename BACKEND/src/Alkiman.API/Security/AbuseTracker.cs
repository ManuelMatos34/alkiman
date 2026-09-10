using System.Collections.Concurrent;

namespace Alkiman.API.Security;

/// <summary>
/// Rastreador en memoria de volumen de requests por IP.
/// Auto-bloquea IPs que superan el umbral de abuso dentro de una ventana de tiempo.
///
/// Complementa el rate limiter de ASP.NET Core (devuelve 429) con un bloqueo
/// temporal más agresivo (devuelve 403 vía IpBlocklistMiddleware) para IPs que
/// ignoran los 429 y siguen insistiendo.
///
/// Es intencional que el estado se pierda al reiniciar la app: los bloqueos
/// temporales no son persistentes. Para bloqueos permanentes usa Security:BlockedIps
/// en la configuración.
/// </summary>
internal static class AbuseTracker
{
    private sealed record IpWindow(int Count, DateTimeOffset WindowStart, DateTimeOffset? BlockedUntil);

    private static readonly ConcurrentDictionary<string, IpWindow> Windows = new(StringComparer.OrdinalIgnoreCase);

    // 500 req/min por IP está muy por encima de cualquier uso legítimo
    private const int AutoBlockThreshold = 500;
    private static readonly TimeSpan TrackingWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(30);

    public static bool IsBlocked(string ip)
    {
        if (!Windows.TryGetValue(ip, out var window) || window.BlockedUntil is null)
            return false;

        if (DateTimeOffset.UtcNow < window.BlockedUntil)
            return true;

        // El bloqueo expiró: limpiar para no acumular entradas obsoletas
        Windows.TryRemove(ip, out _);
        return false;
    }

    public static void Record(string ip)
    {
        Windows.AddOrUpdate(
            key: ip,
            addValueFactory: _ => new IpWindow(1, DateTimeOffset.UtcNow, null),
            updateValueFactory: (_, existing) =>
            {
                var now = DateTimeOffset.UtcNow;

                // Si ya está bloqueada, mantener el bloqueo sin cambiar el contador
                if (existing.BlockedUntil.HasValue && now < existing.BlockedUntil)
                    return existing;

                // Ventana expirada: reiniciar contador
                if (now - existing.WindowStart > TrackingWindow)
                    return new IpWindow(1, now, null);

                var newCount = existing.Count + 1;
                var blockUntil = newCount >= AutoBlockThreshold
                    ? now + BlockDuration
                    : (DateTimeOffset?)null;

                return new IpWindow(newCount, existing.WindowStart, blockUntil);
            });
    }
}
