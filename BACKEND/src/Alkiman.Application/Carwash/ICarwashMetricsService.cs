namespace Alkiman.Application.Carwash;

/// <summary>Tablero de métricas del módulo Carwash para el negocio autenticado.</summary>
public interface ICarwashMetricsService
{
    /// <summary>
    /// Métricas del rango pedido. Ambas fechas son inclusivas y opcionales:
    /// sin nada se devuelven los últimos 30 días.
    /// </summary>
    Task<CarwashMetricsResponse> GetMetricsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
