namespace Alkiman.Application.Carwash;

/// <summary>
/// Acceso de solo lectura a las agregaciones del tablero de Carwash. Toda consulta
/// se filtra por LandlordId y por un rango [from, to) semiabierto: el llamador pasa
/// como <c>to</c> el instante siguiente al último día que quiere incluir, así no hay
/// que jugar con milisegundos ni perder los tickets entregados a las 23:59.
/// </summary>
public interface ICarwashMetricsRepository
{
    Task<CarwashVolumeTotalsRaw> GetVolumeTotalsAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<CarwashRevenueTotalsRaw> GetRevenueTotalsAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarwashDailyPointDto>> GetDailyVolumeAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarwashServiceUsageDto>> GetTopServicesAsync(Guid landlordId, DateTime from, DateTime to, int top, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarwashExtraUsageDto>> GetTopExtrasAsync(Guid landlordId, DateTime from, DateTime to, int top, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarwashHourlyPointDto>> GetHourlyDistributionAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarwashWasherRankingRaw>> GetWasherRankingAsync(Guid landlordId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
